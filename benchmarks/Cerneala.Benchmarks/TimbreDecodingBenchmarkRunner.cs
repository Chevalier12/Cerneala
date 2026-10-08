using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Timbre;
using Cerneala.Timbre.Engine;

namespace Cerneala.Benchmarks;

// Timbre decoding cost and memory (plan 2026-10-03-timbre-decoding-streaming,
// stages 2-3), measured on the production decoder seam with the committed
// corpus. Per scenario (codec × short/long file): open time, first-block
// time, decode-worker cost of 480-frame blocks (1000 warmup + 10000 measured,
// looping the source), throughput, allocations, the cooperative reservation,
// and the live managed heap held by one reader after opening, at its peak
// across decoding, seeks and loop rewinds, and after disposal. Run as 5
// processes by Invoke-TimbreDecodingCost.ps1. Scope: managed heap of the
// reader (stream, demultiplexer, decoder, converter); static decoder tables,
// uncollected garbage, CLR/OS/native memory and stacks are not attributed.
internal static class TimbreDecodingBenchmarkRunner
{
    private const int BlockFrames = 480;
    private const int WarmupBlocks = 1000;
    private const int MeasuredBlocks = 10000;

    private static readonly string[] Scenarios =
    [
        "wav:short", "wav:long",
        "mp3-mpeg2-22050-mono-vbr.mp3", "mp3-long-22050-mono-cbr64.mp3",
        "mp3-mpeg1-44100-stereo-cbr128.mp3",
        "vorbis-22050-mono-q2.ogg", "vorbis-long-22050-mono-q2.ogg",
        "vorbis-44100-stereo-q4.ogg",
        "opus-mono-16000-24k.opus", "opus-long-mono-32k.opus",
        "opus-stereo-48000-96k.opus",
    ];

    public static void Run(string reportPath, string corpus)
    {
        string wavDirectory = Path.Combine(Path.GetTempPath(), "cerneala-timbre-bench");
        Directory.CreateDirectory(wavDirectory);
        List<object> results = [];
        foreach (string scenario in Scenarios)
        {
            string path = scenario switch
            {
                "wav:short" => Wav(wavDirectory, 2),
                "wav:long" => Wav(wavDirectory, 300),
                _ => Path.Combine(corpus, scenario),
            };
            results.Add(Measure(Path.GetFileName(path), path));
        }

        List<object> startup = [.. StreamingStartup(corpus)];
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            process = Environment.GetEnvironmentVariable("TIMBRE_BENCH_PROCESS") ?? "0",
            machine = Environment.MachineName,
            os = RuntimeInformation.OSDescription,
            runtime = RuntimeInformation.FrameworkDescription,
            processors = Environment.ProcessorCount,
            protocol = new { BlockFrames, WarmupBlocks, MeasuredBlocks },
            results,
            streamingStartup = startup,
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    }

    // Play of a streaming file (open, decode, first block) to its first PCM
    // queued at an output that is idle and empty. No approved threshold: the
    // index gate covers prepared clips only.
    private static IEnumerable<object> StreamingStartup(string corpus)
    {
        const int Samples = 20;
        foreach (string name in new[] { "mp3-long-22050-mono-cbr64.mp3", "vorbis-long-22050-mono-q2.ogg", "opus-long-mono-32k.opus" })
        {
            double[] milliseconds = new double[Samples];
            for (int sample = 0; sample < Samples; sample++)
            {
                HeldOutput output = new();
                using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output });
                using TimbreScope scope = runtime.CreateScope();
                TimbreClip clip = new(TimbreSource.FromFile(Path.GetFullPath(Path.Combine(corpus, name))), loading: TimbreLoading.Streaming);
                long started = Stopwatch.GetTimestamp();
                TimbrePlayback playback = scope.Play(clip);
                long queued = output.FirstSubmit.GetAwaiter().GetResult();
                milliseconds[sample] = (queued - started) * 1000.0 / Stopwatch.Frequency;
                playback.Cancel();
            }

            Array.Sort(milliseconds);
            yield return new { file = name, samples = Samples, p50 = Percentile(milliseconds, 0.50), p95 = Percentile(milliseconds, 0.95), max = milliseconds[^1] };
        }
    }

    // Accepts one software queue of PCM, never consumes it, and timestamps the
    // first submission (the first PCM queued).
    private sealed class HeldOutput : ITimbreOutput
    {
        private readonly TaskCompletionSource<long> firstSubmit = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<long> FirstSubmit => firstSubmit.Task;

        public int QueuedFrames { get; private set; }

        public void Open(ITimbreOutputClient client)
        {
        }

        public void Submit(ReadOnlySpan<float> samples)
        {
            firstSubmit.TrySetResult(Stopwatch.GetTimestamp());
            QueuedFrames += samples.Length / 2;
        }

        public void Close()
        {
        }
    }

    private static object Measure(string name, string path)
    {
        // Measurement buffers exist before the baseline: they are not the reader's.
        float[] block = new float[BlockFrames * 2];
        double[] samples = new double[MeasuredBlocks];
        TimbreMemoryPool pool = new(long.MaxValue);
        Collect();
        long baseline = GC.GetTotalMemory(forceFullCollection: true);
        (object Timing, long AfterOpen, long Peak, long Reserved) live = MeasureLive(path, pool, block, samples);
        GC.KeepAlive(block);
        GC.KeepAlive(samples);
        Collect();
        long afterDispose = GC.GetTotalMemory(forceFullCollection: true) - baseline;
        return new
        {
            file = name,
            fileBytes = new FileInfo(path).Length,
            timing = live.Timing,
            reservedBytes = live.Reserved,
            liveHeap = new { afterOpen = live.AfterOpen - baseline, peak = live.Peak - baseline, afterDispose },
        };
    }

    // The reader lives only inside this frame, so after it returns nothing
    // the JIT kept alive can hold the reader graph.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (object Timing, long AfterOpen, long Peak, long Reserved) MeasureLive(string path, TimbreMemoryPool pool, float[] block, double[] samples)
    {
        TimbreMemoryBudget budget = new(pool, path);
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch clock = Stopwatch.StartNew();
        TimbreReader reader = TimbreDecoders.Open(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, TimbreSource.FileBufferBytes), path, budget);
        double openMs = clock.Elapsed.TotalMilliseconds;
        long openAllocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Read(reader, block);
        double firstBlockMs = clock.Elapsed.TotalMilliseconds;
        long reserved = pool.Reserved;
        long afterOpen = Heap();
        long peak = afterOpen;

        // Decode-worker cost per block, rewinding at the end like a loop.
        long rewinds = 0;
        long steadyAllocatedBefore = 0;
        for (int index = 0; index < WarmupBlocks + MeasuredBlocks; index++)
        {
            if (index == WarmupBlocks)
            {
                steadyAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            }

            long start = Stopwatch.GetTimestamp();
            if (!Read(reader, block))
            {
                reader.SeekAsync(0, CancellationToken.None).AsTask().GetAwaiter().GetResult();
                rewinds++;
                Read(reader, block);
            }

            if (index >= WarmupBlocks)
            {
                samples[index - WarmupBlocks] = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
            }

            if (index % 2000 == 0)
            {
                peak = Math.Max(peak, Heap());
            }
        }

        long steadyAllocated = GC.GetAllocatedBytesForCurrentThread() - steadyAllocatedBefore;

        // Seeks across the source and full passes, then the peak again.
        long length = reader.LengthFrames ?? 0;
        clock.Restart();
        long decodedFrames = 0;
        reader.SeekAsync(0, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        while (Read(reader, block))
        {
            decodedFrames += BlockFrames;
        }

        double fullDecodeMs = clock.Elapsed.TotalMilliseconds;
        foreach (long target in new[] { length / 3, length * 9 / 10, 0, length / 2 })
        {
            reader.SeekAsync(target, CancellationToken.None).AsTask().GetAwaiter().GetResult();
            Read(reader, block);
            peak = Math.Max(peak, Heap());
        }

        peak = Math.Max(peak, Heap());
        reader.Dispose();
        budget.Close();
        Array.Sort(samples);
        return (new
        {
            openMs = Math.Round(openMs, 3),
            firstBlockMs = Math.Round(firstBlockMs, 3),
            openAllocatedBytes = openAllocated,
            blockMicroseconds = new { p50 = Percentile(samples, 0.50), p95 = Percentile(samples, 0.95), p99 = Percentile(samples, 0.99), max = samples[^1] },
            steadyAllocatedBytesPerBlock = Math.Round((double)steadyAllocated / MeasuredBlocks, 1),
            loopRewinds = rewinds,
            fullDecodeRealtimeFactor = Math.Round(decodedFrames / 48000.0 / (fullDecodeMs / 1000.0), 1),
        }, afterOpen, peak, reserved);
    }

    private static bool Read(TimbreReader reader, float[] block)
    {
        TimbreReadResult result = reader.ReadAsync(block, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return result.Frames > 0;
    }

    private static double Percentile(double[] sorted, double fraction) =>
        Math.Round(sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(fraction * sorted.Length) - 1)], 2);

    private static long Heap()
    {
        Collect();
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    // 22.05 kHz mono 16-bit sine, the same configuration for both durations.
    private static string Wav(string directory, int seconds)
    {
        string path = Path.Combine(directory, $"bench-22050-mono-{seconds}s.wav");
        if (File.Exists(path))
        {
            return path;
        }

        int frames = 22050 * seconds;
        using BinaryWriter writer = new(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + (frames * 2));
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(22050);
        writer.Write(22050 * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(frames * 2);
        for (int frame = 0; frame < frames; frame++)
        {
            writer.Write((short)(Math.Sin(frame * 2 * Math.PI * 440 / 22050) * 12000));
        }

        return path;
    }
}
