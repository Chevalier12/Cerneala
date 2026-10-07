using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Timbre;
using Cerneala.Timbre.Engine;
using Microsoft.Win32;

namespace Cerneala.Benchmarks;

// Timbre core cost gate (index §7): 32 looping voices (28 preloaded + 4
// streaming), each with LowPass + Delay, 480-frame blocks, 1000 warmup and
// 10000 measured blocks per process. A device emulator consumes PCM on the
// wall clock at 48 kHz, independently of the mixer, and notifies capacity.
internal static class TimbreCoreBenchmarkRunner
{
    private static readonly bool Pilot = Environment.GetEnvironmentVariable("TIMBRE_BENCH_PILOT") == "1";
    private static readonly int WarmupBlocks = Pilot ? 100 : 1000;
    private static readonly int MeasuredBlocks = Pilot ? 500 : 10000;
    private const int PreloadedVoices = 28;
    private const int StreamingVoices = 4;
    private static readonly int LatencySamples = Pilot ? 20 : 200;
    private const double BlockMilliseconds = 10.0;

    public static void Run(string reportPath)
    {
        RunAsync(reportPath).GetAwaiter().GetResult();
    }

    private static async Task RunAsync(string reportPath)
    {
        PacedDevice device = new();
        BlockRecorder recorder = new(WarmupBlocks, MeasuredBlocks);
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = device });
        runtime.BlockObserver = recorder;
        using SoundScope scope = runtime.CreateScope();

        SoundModifier[] chain = [new LowPass(cutoff: 2000f), new Delay(time: 0.25f, feedback: 0.4f, mix: 0.3f)];
        SoundClip[] preloaded = Enumerable.Range(0, 4)
            .Select(index => new SoundClip(
                SoundSource.FromReader(() => new NoiseReader(2 * 48000, seed: index), $"preloaded-{index}"),
                volume: 0.5f,
                loop: true,
                loading: SoundLoading.Preload,
                modifiers: chain))
            .ToArray();
        SoundClip streaming = new(
            SoundSource.FromReader(() => new NoiseReader(60 * 48000, seed: 99), "streaming"),
            volume: 0.5f,
            loop: true,
            loading: SoundLoading.Streaming,
            modifiers: chain);
        // Plain prepared one-shot: completion follows the drain, so each latency
        // sample ends within ~150 ms.
        SoundClip shortClip = new(
            SoundSource.FromReader(() => new NoiseReader(4800, seed: 7), "short"),
            loading: SoundLoading.Preload);
        foreach (SoundClip clip in preloaded)
        {
            await runtime.PrepareAsync(clip);
        }

        await runtime.PrepareAsync(shortClip);
        List<SoundPlayback> voices = [];
        for (int index = 0; index < PreloadedVoices; index++)
        {
            voices.Add(scope.Play(preloaded[index % preloaded.Length]));
        }

        for (int index = 0; index < StreamingVoices; index++)
        {
            voices.Add(scope.Play(streaming));
        }

        device.Start();
        await recorder.WarmedUp;
        SoundRuntimeDiagnostics atWarmup = runtime.GetDiagnostics();
        long deviceUnderrunAtWarmup = device.UnderrunFrames;
        await recorder.Completed;
        SoundRuntimeDiagnostics atEnd = runtime.GetDiagnostics();
        long deviceUnderrunAtEnd = device.UnderrunFrames;
        int maxQueuedMeasured = device.MaxQueuedFrames;

        double[] latencies = new double[LatencySamples];
        for (int sample = 0; sample < LatencySamples; sample++)
        {
            long started = Stopwatch.GetTimestamp();
            SoundPlayback probe = scope.Play(shortClip);
            await probe.Completion;
            latencies[sample] = (probe.FirstQueuedTimestamp - started) * 1000.0 / Stopwatch.Frequency;
        }

        foreach (SoundPlayback voice in voices)
        {
            voice.Cancel();
        }

        device.Stop();
        double[] mixMilliseconds = recorder.MeasuredTicks.Select(ticks => ticks * 1000.0 / Stopwatch.Frequency).ToArray();
        Array.Sort(mixMilliseconds);
        Array.Sort(latencies);
        long allocatedBytes = recorder.MeasuredAllocatedBytes.Sum();
        Report report = new(
            Environment.GetEnvironmentVariable("TIMBRE_BENCH_PROCESS") ?? "0",
            DateTimeOffset.Now.ToString("O"),
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorName(),
            Environment.ProcessorCount,
            PreloadedVoices + StreamingVoices,
            StreamingVoices,
            WarmupBlocks,
            mixMilliseconds.Length,
            Percentile(mixMilliseconds, 0.50),
            Percentile(mixMilliseconds, 0.95),
            Percentile(mixMilliseconds, 0.99),
            mixMilliseconds[^1],
            allocatedBytes,
            recorder.MeasuredAllocatedBytes.Count(bytes => bytes != 0),
            maxQueuedMeasured,
            deviceUnderrunAtEnd - deviceUnderrunAtWarmup,
            atEnd.UnderrunFrames - atWarmup.UnderrunFrames,
            atEnd.UnderrunFrames,
            Percentile(latencies, 0.50),
            Percentile(latencies, 0.95),
            latencies[^1],
            atEnd.CacheBytes,
            atEnd.StreamingBufferBytes,
            atEnd.DspStateBytes,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            new Thresholds(BlockMilliseconds * 0.25, 0, 1920, 0, 50.0));
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, json);
        Console.WriteLine(json);
    }

    private static double Percentile(double[] sorted, double quantile) =>
        sorted[Math.Clamp((int)Math.Ceiling(quantile * sorted.Length) - 1, 0, sorted.Length - 1)];

    private static string ProcessorName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "unknown";
        }

        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        return (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? "unknown";
    }

    private sealed record Thresholds(
        double MaxP99MixMilliseconds,
        long MaxSteadyStateAllocatedBytes,
        int MaxQueuedFrames,
        long MaxUnderrunFrames,
        double MaxPreparedPlayToFirstQueuedP95Milliseconds);

    private sealed record Report(
        string Process,
        string Timestamp,
        string Runtime,
        string OperatingSystem,
        string Architecture,
        string Processor,
        int LogicalProcessors,
        int Voices,
        int StreamingVoices,
        int WarmupBlocks,
        int MeasuredBlocks,
        double MixP50Milliseconds,
        double MixP95Milliseconds,
        double MixP99Milliseconds,
        double MixMaxMilliseconds,
        long MeasuredAllocatedBytes,
        int MeasuredBlocksWithAllocations,
        int MaxQueuedFrames,
        long DeviceUnderrunFramesMeasured,
        long VoiceUnderrunFramesMeasured,
        long VoiceUnderrunFramesTotal,
        double PreparedPlayToFirstQueuedP50Milliseconds,
        double PreparedPlayToFirstQueuedP95Milliseconds,
        double PreparedPlayToFirstQueuedMaxMilliseconds,
        long CacheBytes,
        long StreamingBufferBytes,
        long DspStateBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        Thresholds Gate);

    // Preallocated per-block storage written only by the mixer thread.
    private sealed class BlockRecorder(int warmup, int measured) : ISoundBlockObserver
    {
        private readonly long[] ticks = new long[measured];
        private readonly long[] allocated = new long[measured];
        private readonly TaskCompletionSource warmedUp = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int seen;

        public Task WarmedUp => warmedUp.Task;

        public Task Completed => completed.Task;

        public long[] MeasuredTicks => ticks;

        public long[] MeasuredAllocatedBytes => allocated;

        public void OnBlockSubmitted(long elapsedTicks, long allocatedBytes)
        {
            int index = seen++ - warmup;
            if (index == 0)
            {
                warmedUp.TrySetResult();
            }

            if (index >= 0 && index < ticks.Length)
            {
                ticks[index] = elapsedTicks;
                allocated[index] = allocatedBytes;
                if (index == ticks.Length - 1)
                {
                    completed.TrySetResult();
                }
            }
        }
    }

    // Device emulator: consumes queued PCM at 48 kHz of wall-clock time on its
    // own thread, counts frames it could not consume (device underrun), and
    // notifies the runtime after consuming. It never mixes or processes PCM.
    private sealed class PacedDevice : ISoundOutput
    {
        private readonly object gate = new();
        private ISoundOutputClient? client;
        private Thread? thread;
        private volatile bool running;
        private long submitted;
        private long consumed;
        private long underrun;
        private int maxQueued;

        public int QueuedFrames
        {
            get
            {
                lock (gate)
                {
                    return (int)(submitted - consumed);
                }
            }
        }

        public long UnderrunFrames => Interlocked.Read(ref underrun);

        public int MaxQueuedFrames => Volatile.Read(ref maxQueued);

        public void Open(ISoundOutputClient client)
        {
            lock (gate)
            {
                this.client = client;
            }
        }

        public void Submit(ReadOnlySpan<float> samples)
        {
            lock (gate)
            {
                submitted += samples.Length / SoundRuntime.ChannelCount;
                maxQueued = Math.Max(maxQueued, (int)(submitted - consumed));
            }
        }

        public void Close()
        {
            lock (gate)
            {
                client = null;
            }
        }

        public void Start()
        {
            running = true;
            thread = new Thread(Consume) { IsBackground = true, Name = "Timbre bench device", Priority = ThreadPriority.Highest };
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            thread?.Join();
        }

        private void Consume()
        {
            Stopwatch clock = Stopwatch.StartNew();
            long played = 0;
            while (running)
            {
                Thread.Sleep(1);
                long due = (long)(clock.Elapsed.TotalSeconds * SoundRuntime.SampleRate) - played;
                if (due <= 0)
                {
                    continue;
                }

                ISoundOutputClient? notify;
                lock (gate)
                {
                    long available = submitted - consumed;
                    long take = Math.Min(due, available);
                    consumed += take;
                    if (take < due && played > 0)
                    {
                        underrun += due - take;
                    }

                    notify = client;
                }

                played += due;
                notify?.NotifyCapacityAvailable();
            }
        }
    }

    // Deterministic broadband source; streaming readers generate on demand.
    private sealed class NoiseReader(long lengthFrames, int seed) : SoundReader
    {
        private long position;

        public override long? LengthFrames => lengthFrames;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / SoundRuntime.ChannelCount, lengthFrames - position);
            Fill(destination.Span, frames);
            position += frames;
            return ValueTask.FromResult(new SoundReadResult(frames, position == lengthFrames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            return ValueTask.CompletedTask;
        }

        private void Fill(Span<float> destination, int frames)
        {
            for (int index = 0; index < frames * 2; index++)
            {
                ulong state = ((ulong)((position * 2) + index) + ((ulong)seed * 0x9E3779B97F4A7C15UL)) * 6364136223846793005UL;
                state ^= state >> 29;
                destination[index] = ((state >> 40) / (float)(1UL << 24) - 0.5f) * 0.1f;
            }
        }
    }
}
