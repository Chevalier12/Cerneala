using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;
using Cerneala.Timbre.Engine;
using Microsoft.Win32;

namespace Cerneala.Benchmarks;

// Timbre core cost gate (index §7): 32 looping voices (28 preloaded + 4
// streaming), each with LowPass + Delay, 480-frame blocks, 1000 warmup and
// 10000 measured blocks per process. A device emulator consumes PCM on the
// wall clock at 48 kHz, independently of the mixer, and notifies capacity.
// SDL3 backend plan, stage 3: TIMBRE_BENCH_OUTPUT=sdl replaces the emulator
// with the SDL3 output on the default playback device.
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
        if (Environment.GetEnvironmentVariable("TIMBRE_BENCH_OUTPUT") == "sdl")
        {
            // SDL video/events stay owned by this thread; the output owns audio.
            using SdlPlatformLifetime lifetime = new(new NativeSdlApi());
            RunAsync(reportPath, new SdlTimbreOutput(new NativeSdlAudioApi())).GetAwaiter().GetResult();
            return;
        }

        RunAsync(reportPath, sdlOutput: null).GetAwaiter().GetResult();
    }

    private static async Task RunAsync(string reportPath, SdlTimbreOutput? sdlOutput)
    {
        PacedDevice device = new();
        BlockRecorder recorder = new(WarmupBlocks, MeasuredBlocks);
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = sdlOutput ?? (ITimbreOutput)device });
        runtime.BlockObserver = recorder;
        using TimbreScope scope = runtime.CreateScope();

        TimbreModifier[] chain = [new LowPass(cutoff: 2000f), new Delay(time: 0.25f, feedback: 0.4f, mix: 0.3f)];
        TimbreSound[] preloaded = Enumerable.Range(0, 4)
            .Select(index => new TimbreSound(
                TimbreSource.FromReader(() => new NoiseReader(2 * 48000, seed: index), $"preloaded-{index}"),
                volume: 0.5f,
                loop: true,
                loading: TimbreLoading.Preload,
                modifiers: chain))
            .ToArray();
        TimbreSound streaming = new(
            TimbreSource.FromReader(() => new NoiseReader(60 * 48000, seed: 99), "streaming"),
            volume: 0.5f,
            loop: true,
            loading: TimbreLoading.Streaming,
            modifiers: chain);
        // Decoding plan, stage 3: the same gate with the four streaming voices
        // decoding real corpus files (MP3, Vorbis, Opus, WAV) on their pumps.
        string? decodedCorpus = Environment.GetEnvironmentVariable("TIMBRE_BENCH_DECODED_CORPUS");
        TimbreSound[] streamingClips = decodedCorpus is null
            ? [streaming]
            : DecodedStreamingFiles(decodedCorpus)
                .Select(path => new TimbreSound(TimbreSource.FromFile(path), volume: 0.5f, loop: true, loading: TimbreLoading.Streaming, modifiers: chain))
                .ToArray();
        // Plain prepared one-shot: completion follows the drain, so each latency
        // sample ends within ~150 ms.
        TimbreSound shortClip = new(
            TimbreSource.FromReader(() => new NoiseReader(4800, seed: 7), "short"),
            loading: TimbreLoading.Preload);
        foreach (TimbreSound clip in preloaded)
        {
            await runtime.PrepareAsync(clip);
        }

        await runtime.PrepareAsync(shortClip);
        List<TimbrePlayback> voices = [];
        for (int index = 0; index < PreloadedVoices; index++)
        {
            voices.Add(scope.Play(preloaded[index % preloaded.Length]));
        }

        for (int index = 0; index < StreamingVoices; index++)
        {
            voices.Add(scope.Play(streamingClips[index % streamingClips.Length]));
        }

        if (sdlOutput is null)
        {
            device.Start();
        }

        await recorder.WarmedUp;
        TimbreRuntimeDiagnostics atWarmup = runtime.GetDiagnostics();
        // With SDL the device-side underrun is SDL's estimate of missing input.
        long deviceUnderrunAtWarmup = sdlOutput is null ? device.UnderrunFrames : sdlOutput.GetDiagnostics().StarvedBytes / 8;
        await recorder.Completed;
        TimbreRuntimeDiagnostics atEnd = runtime.GetDiagnostics();
        long deviceUnderrunAtEnd = sdlOutput is null ? device.UnderrunFrames : sdlOutput.GetDiagnostics().StarvedBytes / 8;
        int maxQueuedMeasured = sdlOutput is null ? device.MaxQueuedFrames : sdlOutput.GetDiagnostics().QueueHighWaterFrames;

        double[] latencies = new double[LatencySamples];
        for (int sample = 0; sample < LatencySamples; sample++)
        {
            long started = Stopwatch.GetTimestamp();
            TimbrePlayback probe = scope.Play(shortClip);
            await probe.Completion;
            latencies[sample] = (probe.FirstQueuedTimestamp - started) * 1000.0 / Stopwatch.Frequency;
        }

        foreach (TimbrePlayback voice in voices)
        {
            voice.Cancel();
        }

        device.Stop();
        SdlTimbreOutputDiagnostics? sdl = sdlOutput?.GetDiagnostics();
        string output = sdl is { } opened
            ? $"sdl {SDL3.SDL.GetCurrentAudioDriver()} 0x{opened.DeviceFormat.Format:X}/{opened.DeviceFormat.Channels}/{opened.DeviceFormat.Frequency} {opened.DeviceSampleFrames} frames"
            : "emulator";
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
            output,
            sdl?.OpenCount ?? 0,
            NativeSdlAudioApi.LateCallbacks,
            new Thresholds(BlockMilliseconds * 0.25, 0, 1920, 0, 50.0));
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, json);
        Console.WriteLine(json);
    }

    private static IEnumerable<string> DecodedStreamingFiles(string corpus)
    {
        yield return Path.GetFullPath(Path.Combine(corpus, "mp3-long-22050-mono-cbr64.mp3"));
        yield return Path.GetFullPath(Path.Combine(corpus, "vorbis-long-22050-mono-q2.ogg"));
        yield return Path.GetFullPath(Path.Combine(corpus, "opus-long-mono-32k.opus"));
        yield return Path.GetFullPath(Path.Combine(corpus, "mp3-mpeg1-44100-stereo-cbr128.mp3"));
    }

    internal static double Percentile(double[] sorted, double quantile) =>
        sorted[Math.Clamp((int)Math.Ceiling(quantile * sorted.Length) - 1, 0, sorted.Length - 1)];

    internal static string ProcessorName()
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
        string Output,
        int OutputOpens,
        long LateCallbacks,
        Thresholds Gate);

    // Preallocated per-block storage written only by the mixer thread.
    internal sealed class BlockRecorder(int warmup, int measured) : ITimbreBlockObserver
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
    internal sealed class PacedDevice : ITimbreOutput
    {
        private readonly object gate = new();
        private ITimbreOutputClient? client;
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

        public void Open(ITimbreOutputClient client)
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
                submitted += samples.Length / TimbreRuntime.ChannelCount;
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
                long due = (long)(clock.Elapsed.TotalSeconds * TimbreRuntime.SampleRate) - played;
                if (due <= 0)
                {
                    continue;
                }

                ITimbreOutputClient? notify;
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
    internal sealed class NoiseReader(long lengthFrames, int seed) : TimbreReader
    {
        private long position;

        public override long? LengthFrames => lengthFrames;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / TimbreRuntime.ChannelCount, lengthFrames - position);
            Fill(destination.Span, frames);
            position += frames;
            return ValueTask.FromResult(new TimbreReadResult(frames, position == lengthFrames));
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
