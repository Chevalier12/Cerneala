using System.Text.Json;
using Cerneala.Timbre;
using Cerneala.Timbre.Engine;

namespace Cerneala.Tests.Timbre.Decoding;

// The committed decoding corpus (tests/Cerneala.Tests.Timbre/Corpus) and its
// manifest. Expected lengths and signal alignment come from the generator's
// parameters and the container metadata, never from a decoder.
internal static class DecodingCorpus
{
    public const int Rate = TimbreRuntime.SampleRate;

    // LAME's encoder delay and the MP3 synthesis decoder delay, kept in the
    // output of a stream that carries no gapless (Info/Xing) tag.
    public const int UntaggedMp3Delay = 576 + 529;

    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "Corpus");

    private static readonly Dictionary<string, JsonElement> Entries = Load();

    public static string PathOf(string name) => Path.Combine(Directory, name);

    public static IEnumerable<object[]> Positive() =>
        Entries.Keys.Where(name => Describe(name) is not null).Select(name => new object[] { name });

    public static IEnumerable<object[]> ShortPositive() =>
        Positive().Where(row => !((string)row[0]).Contains("long", StringComparison.Ordinal) && !((string)row[0]).Contains("wrap", StringComparison.Ordinal));

    // Signal description of a positive file, or null for negative fixtures.
    public static CorpusExpectation? Describe(string name)
    {
        JsonElement parameters = Entries[name].GetProperty("Parameters");
        if (parameters.TryGetProperty("derivedFrom", out JsonElement from))
        {
            string source = from.GetString()!;
            string transformation = parameters.GetProperty("transformation").GetString()!;
            if (!Entries.ContainsKey(source) || transformation.Contains("cut", StringComparison.Ordinal) ||
                transformation.Contains("zero", StringComparison.Ordinal) || transformation.Contains("inverted", StringComparison.Ordinal) ||
                transformation.Contains("logical streams", StringComparison.Ordinal))
            {
                return null;
            }

            CorpusExpectation original = Describe(source)!;
            return transformation.Contains("no gapless metadata", StringComparison.Ordinal)
                ? original with { Gapless = false }
                : original;
        }

        if (!parameters.TryGetProperty("codec", out JsonElement codec))
        {
            return null;
        }

        string kind = codec.GetString()!;
        int sampleRate = parameters.GetProperty("sampleRate").GetInt32();
        long frames = kind == "vorbis" ? parameters.GetProperty("finalGranule").GetInt64() : parameters.GetProperty("frames").GetInt64();
        double offset = parameters.TryGetProperty("leadingFramesDropped", out JsonElement dropped) ? dropped.GetInt64() / (double)sampleRate : 0;
        bool gapless = kind != "mp3" || parameters.GetProperty("gapless").GetString()!.StartsWith("LAME", StringComparison.Ordinal);
        return new CorpusExpectation(
            kind,
            sampleRate,
            parameters.GetProperty("channels").GetInt32(),
            frames,
            parameters.GetProperty("period").GetDouble(),
            offset,
            gapless);
    }

    // Opens the production decoder seam exactly as a file source does.
    public static TimbreReader Open(string path, long memoryLimit = long.MaxValue) =>
        Open(path, new TimbreMemoryBudget(new TimbreMemoryPool(memoryLimit), path));

    public static TimbreReader Open(string path, TimbreMemoryBudget budget) =>
        TimbreDecoders.Open(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), path, budget);

    // Reads to the end with the given read sizes; checks the reader contract.
    public static float[] DecodeAll(TimbreReader reader, Func<int, int>? readSize = null)
    {
        List<float> pcm = [];
        float[] buffer = new float[16384 * 2];
        for (int call = 0; ; call++)
        {
            int frames = readSize?.Invoke(call) ?? 8192;
            TimbreReadResult result = reader.ReadAsync(buffer.AsMemory(0, frames * 2), CancellationToken.None).AsTask().GetAwaiter().GetResult();
            Assert.InRange(result.Frames, 0, frames);
            pcm.AddRange(buffer.AsSpan(0, result.Frames * 2).ToArray());
            if (result.EndOfSource)
            {
                break;
            }

            Assert.True(result.Frames > 0, "A decoder reader returned zero frames without the end of the source.");
        }

        for (int extra = 0; extra < 3; extra++)
        {
            TimbreReadResult after = reader.ReadAsync(buffer.AsMemory(0, 2048), CancellationToken.None).AsTask().GetAwaiter().GetResult();
            Assert.Equal(new TimbreReadResult(0, endOfSource: true), after);
        }

        return [.. pcm];
    }

    private static Dictionary<string, JsonElement> Load()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Directory, "corpus-manifest.json")));
        return document.RootElement.GetProperty("files").EnumerateArray()
            .ToDictionary(entry => entry.GetProperty("Name").GetString()!, entry => entry.Clone());
    }
}

internal sealed record CorpusExpectation(string Codec, int SampleRate, int Channels, long SourceFrames, double Period, double OffsetSeconds, bool Gapless)
{
    // Canonical frames: ceil(sourceFrames · 48000 / rate).
    public long CanonicalFrames => ((SourceFrames * DecodingCorpus.Rate) + SampleRate - 1) / SampleRate;

    // Seconds of signal at canonical frame 0 (negative while an untrimmed
    // MP3 still plays its encoder and decoder delay).
    public double StartSeconds => Gapless ? OffsetSeconds : OffsetSeconds - ((double)DecodingCorpus.UntaggedMp3Delay / SampleRate);

    // Duration of the signal the generator encoded; the oracle is silent after it.
    public double SignalSeconds => ((double)SourceFrames / SampleRate) + OffsetSeconds;
}
