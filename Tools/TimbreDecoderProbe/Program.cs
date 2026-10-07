using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Cerneala.Timbre.Corpus;
using Cerneala.Timbre.Decoding;
using Cerneala.Timbre.Probe;

// Stage-0 decoder candidate probe:
//   dotnet run --project Tools/TimbreDecoderProbe -c Release -- <corpus dir> <result json>
// Every candidate is wrapped as a DecodedSource and converted by the same
// CanonicalConverter; PCM is checked against CorpusSignal, never against
// another decoder.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
string corpus = Path.GetFullPath(args.Length > 0 ? args[0] : "tests/Cerneala.Tests.Timbre/Corpus");
string resultPath = Path.GetFullPath(args.Length > 1 ? args[1] : "probe-results.json");
string wavDirectory = Path.Combine(Path.GetTempPath(), "timbre-probe-wav");
Directory.CreateDirectory(wavDirectory);
JsonElement manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "corpus-manifest.json"))).RootElement;

List<(string Path, double Period, string Candidate, Func<Stream, DecodedSource> Open, double Offset, long Expected)> runs = [];
foreach (JsonElement entry in manifest.GetProperty("files").EnumerateArray())
{
    JsonElement parameters = entry.GetProperty("Parameters");
    if (!parameters.TryGetProperty("codec", out JsonElement codec))
    {
        continue;
    }

    string path = Path.Combine(corpus, entry.GetProperty("Name").GetString()!);
    double period = parameters.GetProperty("period").GetDouble();
    int channels = parameters.GetProperty("channels").GetInt32();
    int sourceRate = parameters.GetProperty("sampleRate").GetInt32();
    long sourceFrames = parameters.TryGetProperty("finalGranule", out JsonElement final) && codec.GetString() == "vorbis" ? final.GetInt64() : parameters.GetProperty("frames").GetInt64();
    double offset = parameters.TryGetProperty("leadingFramesDropped", out JsonElement dropped) ? dropped.GetInt64() / (double)sourceRate : 0;
    long expected = ((sourceFrames * 48000) + sourceRate - 1) / sourceRate;
    switch (codec.GetString())
    {
        case "mp3":
            runs.Add((path, period, "Timbre demux + NLayer 3.0.0 MpegFrameDecoder", s => new Mp3Source(s, path), offset, expected));
            runs.Add((path, period, "NLayer 3.0.0 MpegFile", s => new NLayerMpegFileCandidate(s), offset, expected));
            break;
        case "vorbis":
            runs.Add((path, period, "Timbre Ogg + NVorbis 0.10.5 StreamDecoder", s => new VorbisSource(s, path), offset, expected));
            runs.Add((path, period, "NVorbis 0.10.5 VorbisReader", s => new NVorbisReaderCandidate(s), offset, expected));
            break;
        case "opus":
            runs.Add((path, period, "Timbre Ogg + Concentus 2.2.2 OpusDecoder", s => new OpusSource(s, path), offset, expected));
            runs.Add((path, period, "Concentus.OggFile 1.0.7 OpusOggReadStream", s => new ConcentusOggFileCandidate(s, channels), offset, expected));
            break;
    }
}

foreach ((int rate, int channels, int bits, bool isFloat, double seconds, double period) in WavMatrix())
{
    string path = Path.Combine(wavDirectory, $"wav-{rate}-{(channels == 1 ? "mono" : "stereo")}-{(isFloat ? "f32" : $"s{bits}")}-{seconds:0}s.wav");
    if (!File.Exists(path))
    {
        WriteWav(path, rate, channels, bits, isFloat, seconds, period);
    }

    long wavExpected = (((long)Math.Round(seconds * rate) * 48000) + rate - 1) / rate;
    runs.Add((path, period, "Timbre RIFF reader", s => new WavSource(s, path), 0, wavExpected));
    runs.Add((path, period, "NAudio.Core 2.4.0 WaveFileReader", s => new NAudioWavCandidate(s), 0, wavExpected));
}

string? filter = args.Length > 2 ? args[2] : null;
static string Key(string path, string candidate) => $"{Path.GetFileName(path)} | {candidate}";
if (filter == "--footprint")
{
    List<object> footprints = [.. Footprint.Measure(corpus)];
    File.WriteAllText(resultPath, JsonSerializer.Serialize(new { machine = Environment.MachineName, runtime = Environment.Version.ToString(), footprints }, JsonOptions.Indented) + Environment.NewLine);
    return 0;
}

if (filter == "--list")
{
    foreach (var run in runs)
    {
        Console.WriteLine(Key(run.Path, run.Candidate));
    }

    return 0;
}

List<object> results = [];
foreach ((string path, double period, string candidate, Func<Stream, DecodedSource> open, double offset, long expected) in runs.Where(run => filter is null || (filter.StartsWith('=') ? Key(run.Path, run.Candidate) == filter[1..] : Key(run.Path, run.Candidate).Contains(filter, StringComparison.OrdinalIgnoreCase))))
{
    object result;
    try
    {
        result = Measure(path, period, candidate, open, offset, expected);
    }
    catch (Exception exception)
    {
        result = new { file = Path.GetFileName(path), candidate, error = $"{exception.GetType().Name}: {exception.Message}" };
    }

    results.Add(result);
    Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions.Compact));
}

File.WriteAllText(resultPath, JsonSerializer.Serialize(new
{
    machine = Environment.MachineName,
    os = Environment.OSVersion.ToString(),
    runtime = Environment.Version.ToString(),
    processors = Environment.ProcessorCount,
    corpus,
    results,
}, JsonOptions.Indented) + Environment.NewLine);
return 0;

static object Measure(string path, double period, string candidate, Func<Stream, DecodedSource> open, double offset, long expectedFrames)
{
    const int Block = 2048;
    const int Window = 9600;
    float[] block = new float[Block * 2];
    long fileBytes = new FileInfo(path).Length;
    ForceCollect();
    long baselineHeap = GC.GetTotalMemory(true);
    long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    CountingStream stream = new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096));
    Stopwatch clock = Stopwatch.StartNew();
    CanonicalConverter converter = new(open(stream));
    double openMs = clock.Elapsed.TotalMilliseconds;
    long openAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    long openBytes = stream.BytesRead;
    long? declaredLength = converter.LengthFrames;

    // Continuous decode, streaming the oracle metrics.
    OracleAccumulator oracle = new(period, offset);
    long frames = 0;
    double firstOutputMs = -1;
    long firstOutputBytes = -1;
    double maxCallMs = 0;
    long[] targets = [];
    Dictionary<long, float[]> windows = [];
    float[] firstWindow = new float[Window * 2];
    long heapAfterFirstSecond = 0;
    while (true)
    {
        double before = clock.Elapsed.TotalMilliseconds;
        int read = converter.Read(block, CancellationToken.None);
        maxCallMs = Math.Max(maxCallMs, clock.Elapsed.TotalMilliseconds - before);
        if (read == 0)
        {
            break;
        }

        if (firstOutputMs < 0)
        {
            firstOutputMs = clock.Elapsed.TotalMilliseconds;
            firstOutputBytes = stream.BytesRead;
            long length = declaredLength ?? 0;
            targets = length > Window * 4 ? [length / 10, length / 2, (length * 9) / 10] : [];
            foreach (long target in targets)
            {
                windows[target] = new float[Window * 2];
            }
        }

        oracle.Add(block.AsSpan(0, read * 2), frames);
        Capture(block.AsSpan(0, read * 2), frames, 0, firstWindow);
        foreach ((long target, float[] window) in windows)
        {
            Capture(block.AsSpan(0, read * 2), frames, target, window);
        }

        frames += read;
        if (heapAfterFirstSecond == 0 && frames >= 48000)
        {
            heapAfterFirstSecond = GC.GetTotalMemory(true) - baselineHeap;
        }
    }

    double decodeMs = clock.Elapsed.TotalMilliseconds;
    long decodeAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    long bytesAfterDecode = stream.BytesRead;
    bool eofStable = converter.Read(block, CancellationToken.None) == 0 && converter.Read(block, CancellationToken.None) == 0;
    long retainedAfterDecode = GC.GetTotalMemory(true) - baselineHeap;

    // Seeks against the continuous decode, and the oracle at the target.
    List<object> seeks = [];
    foreach (long target in targets)
    {
        long bytesBefore = stream.BytesRead;
        long seeksBefore = stream.SeekCalls;
        double before = clock.Elapsed.TotalMilliseconds;
        string? error = null;
        float maxDiff = float.NaN;
        OracleAccumulator local = new(period, offset, lagWindowStart: 1600, lagWindowFrames: 6400);
        try
        {
            converter.Seek(target, CancellationToken.None);
            float[] window = ReadWindow(converter, Window);
            maxDiff = MaxDifference(window, windows[target]);
            local.Add(window, target);
        }
        catch (Exception exception)
        {
            error = $"{exception.GetType().Name}: {exception.Message}";
        }

        seeks.Add(new
        {
            target,
            ms = Math.Round(clock.Elapsed.TotalMilliseconds - before, 3),
            bytesRead = stream.BytesRead - bytesBefore,
            streamSeeks = stream.SeekCalls - seeksBefore,
            maxDiffVsContinuous = maxDiff,
            snrDb = Math.Round(local.SnrDb, 2),
            lag = local.Lag(),
            error,
        });
    }

    // Loop: three rewinds to 0 and full passes; retained heap must not grow.
    List<object> loops = [];
    for (int pass = 0; pass < 3; pass++)
    {
        string? error = null;
        float diff = float.NaN;
        long passFrames = 0;
        try
        {
            converter.Seek(0, CancellationToken.None);
            float[] window = ReadWindow(converter, Window);
            diff = MaxDifference(window, firstWindow);
            passFrames = Window;
            int read;
            while ((read = converter.Read(block, CancellationToken.None)) > 0)
            {
                passFrames += read;
            }
        }
        catch (Exception exception)
        {
            error = $"{exception.GetType().Name}: {exception.Message}";
        }

        loops.Add(new { pass, frames = passFrames, maxDiffVsFirstPass = diff, retainedBytes = GC.GetTotalMemory(true) - baselineHeap, error });
    }

    converter.Dispose();
    long retainedAfterDispose = GC.GetTotalMemory(true) - baselineHeap;
    return new
    {
        file = Path.GetFileName(path),
        candidate,
        fileBytes,
        declaredLength,
        frames,
        expectedFrames,
        open = new { ms = Math.Round(openMs, 3), allocatedBytes = openAllocated, bytesRead = openBytes },
        firstOutput = new { ms = Math.Round(firstOutputMs, 3), bytesRead = firstOutputBytes, fractionOfFile = Math.Round((double)firstOutputBytes / fileBytes, 4) },
        decode = new
        {
            ms = Math.Round(decodeMs, 1),
            realtimeFactor = Math.Round(frames / 48000.0 / (decodeMs / 1000.0), 1),
            allocatedBytes = decodeAllocated,
            bytesRead = bytesAfterDecode,
            maxCallMs = Math.Round(maxCallMs, 3),
        },
        oracle = new { snrDbLeft = Math.Round(oracle.ChannelSnrDb(0), 2), snrDbRight = Math.Round(oracle.ChannelSnrDb(1), 2), lag = oracle.Lag(), swappedLag = oracle.SwappedScore() },
        eofStable,
        retainedHeap = new { afterFirstSecond = heapAfterFirstSecond, afterFullDecode = retainedAfterDecode, afterDispose = retainedAfterDispose },
        seeks,
        loops,
        streamDisposed = stream.Disposed,
    };
}

static float[] ReadWindow(CanonicalConverter converter, int frames)
{
    float[] window = new float[frames * 2];
    int filled = 0;
    while (filled < frames)
    {
        int read = converter.Read(window.AsSpan(filled * 2), CancellationToken.None);
        if (read == 0)
        {
            break;
        }

        filled += read;
    }

    return window;
}

static void Capture(ReadOnlySpan<float> samples, long start, long target, float[] window)
{
    long end = start + (samples.Length / 2);
    long windowEnd = target + (window.Length / 2);
    long from = Math.Max(start, target);
    long to = Math.Min(end, windowEnd);
    if (from < to)
    {
        samples.Slice((int)(from - start) * 2, (int)(to - from) * 2).CopyTo(window.AsSpan((int)(from - target) * 2));
    }
}

static float MaxDifference(float[] a, float[] b)
{
    float max = 0;
    for (int index = 0; index < a.Length; index++)
    {
        max = Math.Max(max, Math.Abs(a[index] - b[index]));
    }

    return max;
}

static void ForceCollect()
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
}

static IEnumerable<(int Rate, int Channels, int Bits, bool IsFloat, double Seconds, double Period)> WavMatrix()
{
    foreach ((int bits, bool isFloat) in new[] { (8, false), (16, false), (24, false), (32, false), (32, true) })
    {
        yield return (44100, 2, bits, isFloat, 2.0, 2.0);
    }

    foreach (int rate in new[] { 8000, 22050, 48000, 96000, 192000 })
    {
        yield return (rate, 1, 16, false, 2.0, 2.0);
        yield return (rate, 2, 24, false, 2.0, 2.0);
    }

    yield return (22050, 1, 16, false, 300.0, 10.0);
}

static void WriteWav(string path, int rate, int channels, int bits, bool isFloat, double seconds, double period)
{
    long frames = (long)Math.Round(seconds * rate);
    float[] samples = CorpusSignal.Render(rate, channels, frames, period);
    int bytesPerSample = bits / 8;
    using BinaryWriter writer = new(File.Create(path));
    long dataBytes = samples.Length * (long)bytesPerSample;
    writer.Write("RIFF"u8);
    writer.Write((uint)(36 + dataBytes));
    writer.Write("WAVEfmt "u8);
    writer.Write(16);
    writer.Write((short)(isFloat ? 3 : 1));
    writer.Write((short)channels);
    writer.Write(rate);
    writer.Write(rate * channels * bytesPerSample);
    writer.Write((short)(channels * bytesPerSample));
    writer.Write((short)bits);
    writer.Write("data"u8);
    writer.Write((uint)dataBytes);
    foreach (float sample in samples)
    {
        switch (bits)
        {
            case 8:
                writer.Write((byte)Math.Clamp(Math.Round((sample * 128.0) + 128.0), 0, 255));
                break;
            case 16:
                writer.Write((short)Math.Round(sample * 32767.0));
                break;
            case 24:
                int value = (int)Math.Round(sample * 8388607.0);
                writer.Write((byte)value);
                writer.Write((byte)(value >> 8));
                writer.Write((byte)(value >> 16));
                break;
            case 32 when isFloat:
                writer.Write(sample);
                break;
            default:
                writer.Write((int)Math.Round(sample * 2147483647.0));
                break;
        }
    }
}

// Streaming comparison of canonical output with CorpusSignal at 48 kHz.
internal sealed class OracleAccumulator(double period, double offset, int lagWindowStart = 4800, int lagWindowFrames = 9600)
{
    private readonly int LagWindowStart = lagWindowStart;
    private readonly int LagWindowFrames = lagWindowFrames;
    private const int MaxLag = 1600;
    private readonly double[] error = new double[2];
    private readonly double[] energy = new double[2];
    private readonly List<float> lagWindow = [];
    private long lagWindowOrigin = -1;
    private bool mono = true;

    internal double SnrDb => 10 * Math.Log10((energy[0] + energy[1]) / Math.Max(1e-30, error[0] + error[1]));

    internal double ChannelSnrDb(int channel) => 10 * Math.Log10(energy[channel] / Math.Max(1e-30, error[channel]));

    internal void Add(ReadOnlySpan<float> samples, long start)
    {
        for (int frame = 0; frame < samples.Length / 2; frame++)
        {
            double seconds = ((start + frame) / 48000.0) + offset;
            float left = samples[frame * 2];
            float right = samples[(frame * 2) + 1];
            mono &= left == right;
            double expectedLeft = CorpusSignal.Sample(0, seconds, period);
            double expectedRight = mono ? expectedLeft : CorpusSignal.Sample(1, seconds, period);
            error[0] += (left - expectedLeft) * (left - expectedLeft);
            error[1] += (right - expectedRight) * (right - expectedRight);
            energy[0] += expectedLeft * expectedLeft;
            energy[1] += expectedRight * expectedRight;
            if (lagWindowOrigin < 0)
            {
                lagWindowOrigin = start;
            }

            long relative = start + frame - lagWindowOrigin;
            if (relative >= LagWindowStart - MaxLag && relative < LagWindowStart + LagWindowFrames + MaxLag)
            {
                lagWindow.Add(left);
            }
        }
    }

    // Offset (in 48 kHz frames) at which the left channel best matches the
    // oracle: positive means the output is late.
    internal int Lag()
    {
        if (lagWindow.Count < LagWindowFrames + (2 * MaxLag))
        {
            return int.MinValue;
        }

        int best = 0;
        double bestScore = double.NegativeInfinity;
        for (int lag = -MaxLag; lag <= MaxLag; lag++)
        {
            double score = 0;
            for (int index = 0; index < LagWindowFrames; index += 2)
            {
                double seconds = ((lagWindowOrigin + LagWindowStart + index) / 48000.0) + offset;
                score += lagWindow[MaxLag + index + lag] * CorpusSignal.Sample(0, seconds, period);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = lag;
            }
        }

        return best;
    }

    // Correlation of the left output with the right-channel oracle relative
    // to the left oracle: near 0 when channels are not swapped.
    internal double SwappedScore()
    {
        if (lagWindow.Count < LagWindowFrames + (2 * MaxLag))
        {
            return double.NaN;
        }

        double own = 0;
        double swapped = 0;
        for (int index = 0; index < LagWindowFrames; index++)
        {
            double seconds = ((lagWindowOrigin + LagWindowStart + index) / 48000.0) + offset;
            own += lagWindow[MaxLag + index] * CorpusSignal.Sample(0, seconds, period);
            swapped += lagWindow[MaxLag + index] * CorpusSignal.Sample(1, seconds, period);
        }

        return Math.Round(swapped / own, 4);
    }
}

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions Compact = new() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
    internal static readonly JsonSerializerOptions Indented = new(Compact) { WriteIndented = true };
}
