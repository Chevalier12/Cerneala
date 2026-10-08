using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using SDL3;

namespace Cerneala.SdlGpuSmoke;

// Native Timbre smoke. Playback is driven only through the public C# sound API
// against the real SDL3 output. A test-owned PCM tap sits in front of the
// platform output: each isolated clip's tapped PCM is compared with the same
// engine rendered into a deterministic sink. The tap certifies the pipeline up
// to the output; it does not observe what the device or DAC played.
internal sealed class TimbreSmoke
{
    private const string AbsentDriver = "cerneala-absent-driver";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly SmokeOptions options;
    private readonly MainWindow main;
    private readonly PcmTapOutput tap;
    private readonly SdlTimbreOutput output;
    private readonly TimbreRuntime runtime;
    private readonly List<Dictionary<string, object?>> results = [];

    private TimbreSmoke(SmokeOptions options, MainWindow main, PcmTapOutput tap, SdlTimbreOutput output, TimbreRuntime runtime)
    {
        this.options = options;
        this.main = main;
        this.tap = tap;
        this.output = output;
        this.runtime = runtime;
    }

    internal static PcmTapOutput? InstalledTap { get; private set; }

    private string Fixtures => Path.Combine(AppContext.BaseDirectory, "timbre");

    // From App.OnStartup, before any window takes Application.TimbreRuntime.
    public static void Install(Application application)
    {
        ITimbreOutput platformOutput = WindowApplicationRuntime.Current?.TimbreOutput ??
            throw new InvalidOperationException("The SDL window platform supplies no audio output.");
        InstalledTap = new PcmTapOutput(platformOutput);
        application.TimbreRuntime = new TimbreRuntime(new TimbreRuntimeOptions { Output = InstalledTap });
    }

    public static async Task RunAsync(SmokeOptions options, MainWindow main)
    {
        Application application = Application.Current ?? throw new InvalidOperationException("No Application.");
        PcmTapOutput tap = InstalledTap ?? throw new InvalidOperationException("The PCM tap was not installed at startup.");
        SdlTimbreOutput output = (SdlTimbreOutput)tap.Inner;
        TimbreSmoke smoke = new(options, main, tap, output, application.TimbreRuntime);
        string errorPath = Path.Combine(options.ArtifactDirectory, "timbre.error.txt");
        try
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            await smoke.RunScenariosAsync(application);
            smoke.WriteDiagnostics(failure: null);
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=timbre scenarios={smoke.results.Count} " +
                $"device={output.GetDiagnostics().DeviceFormat} opens={output.GetDiagnostics().OpenCount}");
            application.TimbreRuntime.Dispose();
            main.Close();
        }
        catch (Exception exception)
        {
            smoke.WriteDiagnostics(exception);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL timbre: {exception}");
            application.TimbreRuntime.Dispose();
            application.Shutdown(2);
        }
    }

    private async Task RunScenariosAsync(Application application)
    {
        TimbreScope sounds = application.Timbre;
        string tone = WriteWav(Path.Combine(options.ArtifactDirectory, "timbre-tone-44100.wav"), 44100, 0.5, 330);
        string shortTone = WriteWav(Path.Combine(options.ArtifactDirectory, "timbre-loop-48000.wav"), 48000, 0.25, 550);

        await AbsentDeviceFailsWithoutRetryAsync(sounds, tone);

        string[] formats =
        [
            tone,
            Path.Combine(Fixtures, "mp3-mpeg1-44100-stereo-cbr128.mp3"),
            Path.Combine(Fixtures, "vorbis-48000-stereo-q2.ogg"),
            Path.Combine(Fixtures, "opus-stereo-48000-96k.opus")
        ];
        foreach (string path in formats)
        {
            foreach (TimbreLoading loading in new[] { TimbreLoading.Auto, TimbreLoading.Streaming })
            {
                await CompareAsync(
                    $"{Path.GetFileName(path)}/{loading}",
                    sounds,
                    new TimbreSound(TimbreSource.FromFile(path), loading: loading),
                    new TimbreSound(TimbreSource.FromFile(path), loading: TimbreLoading.Preload));
            }
        }

        TimbreParameter<float> cutoff = new("ToneCutoff", 1200f);
        TimbreModifier[] chain = [new LowPass(cutoff), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)];
        await CompareAsync(
            "modified/LowPass+Delay",
            sounds,
            new TimbreSound(TimbreSource.FromFile(tone), volume: 0.8f, parameters: [cutoff], modifiers: chain),
            new TimbreSound(TimbreSource.FromFile(tone), volume: 0.8f, loading: TimbreLoading.Preload, parameters: [cutoff], modifiers: chain),
            start => start.Set(cutoff, 800f));

        await OverlapAsync(sounds, tone);
        await ReplacementAndCancelAsync(sounds, tone);
        await PausePendingAsync(sounds);
        await PauseResumeAsync(sounds);
        await SeekStreamingAsync(sounds);
        await LoopAsync(sounds, shortTone);
        await TwoWindowsAndQueuedVoicesAsync(tone);
        await DeviceRemovalFailsWithoutReconnectingAsync(sounds, tone);
    }

    private async Task AbsentDeviceFailsWithoutRetryAsync(TimbreScope sounds, string tone)
    {
        Require(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, AbsentDriver, SDL.HintPriority.Override), "Could not select the absent audio driver.");
        TimbrePlaybackResult failed;
        try
        {
            failed = await sounds.Play(new TimbreSound(TimbreSource.FromFile(tone))).Completion.WaitAsync(Timeout);
        }
        finally
        {
            SDL.ResetHint(SDL.Hints.AudioDriver);
        }

        Require(failed.State == TimbrePlaybackState.Failed, $"Absent device: expected Failed, got {failed.State}.");
        Require(failed.Error?.Kind == TimbreErrorKind.DeviceUnavailable, $"Absent device: expected DeviceUnavailable, got {failed.Error?.Kind}.");
        Require(output.GetDiagnostics().OpenCount == 0, "Absent device: an output was opened.");

        // No automatic retry: only an explicit Play opens the default device.
        TimbrePlaybackResult explicitRetry = await sounds.Play(new TimbreSound(TimbreSource.FromFile(tone))).Completion.WaitAsync(Timeout);
        Require(explicitRetry.State == TimbrePlaybackState.Completed, $"Explicit retry: expected Completed, got {explicitRetry.State}.");
        Require(output.GetDiagnostics().OpenCount == 1, "Explicit retry did not open the device exactly once.");
        Record("device/absent-then-explicit-play",
            ("failed", failed.State), ("error", failed.Error?.InnerException?.Message), ("retry", explicitRetry.State));
    }

    private async Task CompareAsync(
        string name,
        TimbreScope sounds,
        TimbreSound clip,
        TimbreSound oracleClip,
        Action<TimbreStartOptions>? configure = null)
    {
        long underrunsBefore = runtime.GetDiagnostics().UnderrunFrames;
        long start = tap.SampleCount;
        Stopwatch watch = Stopwatch.StartNew();
        TimbrePlayback playback = sounds.Play(clip, configure);
        TimbrePlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        watch.Stop();
        float[] actual = tap.Copy(start, tap.SampleCount);
        float[] expected = await RenderOfflineAsync(oracleClip, configure);
        long underruns = runtime.GetDiagnostics().UnderrunFrames - underrunsBefore;

        Require(result.State == TimbrePlaybackState.Completed, $"{name}: expected Completed, got {result.State} ({result.Error?.Message}).");
        Require(underruns == 0, $"{name}: {underruns} underrun frames were padded.");
        int firstDifference = FirstDifference(actual, expected);
        Require(
            actual.Length == expected.Length && firstDifference < 0,
            $"{name}: tapped PCM ({actual.Length} samples) differs from the deterministic sink ({expected.Length}) at sample {firstDifference}.");
        Record(name,
            ("state", result.State),
            ("frames", actual.Length / 2),
            ("wallMs", watch.Elapsed.TotalMilliseconds),
            ("sha256", Convert.ToHexString(SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(actual.AsSpan())))),
            ("matchesSink", true),
            ("tailTruncated", result.TailTruncated));
    }

    private async Task OverlapAsync(TimbreScope sounds, string tone)
    {
        TimbreSound clip = new(TimbreSource.FromFile(tone));
        TimbrePlayback first = sounds.Play(clip);
        await WaitForRequestsAsync(5);
        TimbrePlayback second = sounds.Play(clip);
        TimbrePlaybackResult[] done = await Task.WhenAll(first.Completion, second.Completion).WaitAsync(Timeout);
        Require(done.All(result => result.State == TimbrePlaybackState.Completed), "Overlap: both playbacks must complete.");
        Record("overlap", ("first", done[0].State), ("second", done[1].State));
    }

    private async Task ReplacementAndCancelAsync(TimbreScope sounds, string tone)
    {
        TimbreHandle slot = sounds.CreateHandle();
        TimbrePlayback replaced = sounds.Play(Tone(96000, 220), handle: slot);
        await WaitForTapGrowthAsync(4800);
        TimbrePlayback replacement = sounds.Play(new TimbreSound(TimbreSource.FromFile(tone)), handle: slot);
        Require(replaced.State == TimbrePlaybackState.Canceled, "Replacement must cancel the previous occupant.");
        Require((await replacement.Completion.WaitAsync(Timeout)).State == TimbrePlaybackState.Completed, "Replacement must complete.");

        int opens = output.GetDiagnostics().OpenCount;
        TimbrePlayback canceled = sounds.Play(Tone(96000, 247));
        await WaitForTapGrowthAsync(4800);
        canceled.Cancel();
        canceled.Cancel();
        Require((await canceled.Completion).State == TimbrePlaybackState.Canceled, "Cancel must end Canceled.");
        Require(output.GetDiagnostics().OpenCount == opens && output.GetDiagnostics().IsOpen, "A local cancel closed the shared output.");
        Record("replacement+cancel", ("replaced", replaced.State), ("replacement", replacement.State), ("canceled", canceled.State));
    }

    private async Task PausePendingAsync(TimbreScope sounds)
    {
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TimbrePlayback playback = sounds.Play(new TimbreSound(
            TimbreSource.FromReader(() => new ToneReader(24000, 392, gate.Task), "gated-tone"),
            loading: TimbreLoading.Preload));
        Require(playback.State == TimbrePlaybackState.Pending, "Gated playback must start Pending.");
        playback.Pause();
        long tapped = tap.SampleCount;
        gate.SetResult();
        await WaitForRequestsAsync(30);
        Require(tap.SampleCount == tapped, "Pause in Pending let PCM through before Resume.");
        Require(playback.State == TimbrePlaybackState.Paused, $"Pending pause: expected Paused, got {playback.State}.");

        playback.Resume();
        TimbrePlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        Require(result.State == TimbrePlaybackState.Completed, "Resumed pending playback must complete.");
        Require(tap.SampleCount - tapped >= 24000 * 2, "Resumed pending playback lost PCM.");
        Record("pause-pending", ("state", result.State));
    }

    private async Task PauseResumeAsync(TimbreScope sounds)
    {
        TimbrePlayback playback = sounds.Play(Tone(48000, 294));
        await WaitForTapGrowthAsync(9600);
        playback.Pause();
        await runtime.SyncAsync().WaitAsync(Timeout);
        long tapped = tap.SampleCount;
        TimeSpan position = playback.Position;
        await WaitForRequestsAsync(30);
        Require(tap.SampleCount == tapped, "Paused playback still produced PCM.");
        Require(playback.State == TimbrePlaybackState.Paused && playback.Position == position, "Pause did not hold the position.");
        playback.Resume();
        playback.Resume();
        TimbrePlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        Require(result.State == TimbrePlaybackState.Completed, "Resumed playback must complete.");
        Record("pause-resume", ("pausedAtMs", position.TotalMilliseconds), ("state", result.State));
    }

    private async Task SeekStreamingAsync(TimbreScope sounds)
    {
        TimbrePlayback playback = sounds.Play(new TimbreSound(
            TimbreSource.FromFile(Path.Combine(Fixtures, "mp3-long-22050-mono-cbr64.mp3")),
            loading: TimbreLoading.Streaming));
        await WaitForTapGrowthAsync(9600);
        Task superseded = playback.SeekAsync(TimeSpan.FromSeconds(30));
        Task latest = playback.SeekAsync(TimeSpan.FromSeconds(60));
        await latest.WaitAsync(Timeout);
        bool supersededCanceled = await IsCanceledAsync(superseded);
        await WaitForTapGrowthAsync(9600);
        TimeSpan position = playback.Position;
        Require(position >= TimeSpan.FromSeconds(60), $"Seek: position {position} is before the latest target.");
        var diagnostics = runtime.GetDiagnostics();
        playback.Cancel();
        await playback.Completion.WaitAsync(Timeout);
        Record("seek/streaming-mp3",
            ("position", position.TotalSeconds),
            ("duration", playback.Duration?.TotalSeconds),
            ("supersededCompletedAs", supersededCanceled ? "canceled" : superseded.Status.ToString()),
            ("streamingBufferBytes", diagnostics.StreamingBufferBytes),
            ("readerMemoryBytes", diagnostics.ReaderMemoryBytes));
    }

    private async Task LoopAsync(TimbreScope sounds, string shortTone)
    {
        long wrapsBefore = runtime.GetDiagnostics().LoopWraps;
        TimbrePlayback playback = sounds.Play(new TimbreSound(TimbreSource.FromFile(shortTone), loop: true));
        await WaitForTapGrowthAsync(12000 * 3);
        Require(playback.State == TimbrePlaybackState.Playing, "A looping playback ended on its own.");
        playback.Cancel();
        TimbrePlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        long wraps = runtime.GetDiagnostics().LoopWraps - wrapsBefore;
        Require(result.State == TimbrePlaybackState.Canceled && wraps >= 2, $"Loop: state {result.State}, wraps {wraps}.");
        Record("loop", ("wraps", wraps), ("state", result.State));
    }

    private async Task TwoWindowsAndQueuedVoicesAsync(string tone)
    {
        Window secondary = new() { Title = "Cerneala Timbre smoke secondary", Width = 360, Height = 220, Left = 60, Top = 60 };
        secondary.Show();
        TimbrePlayback fromSecondary = secondary.Timbre.Play(Tone(96000, 262));
        TimbrePlayback fromMain = main.Timbre.Play(Tone(48000, 330));
        await WaitForTapGrowthAsync(4800);
        Require(output.GetDiagnostics().QueueHighWaterFrames <= 1920, "The software queue exceeded 40 ms.");
        int opens = output.GetDiagnostics().OpenCount;

        secondary.Close();
        Require(fromSecondary.State == TimbrePlaybackState.Canceled, "Closing a window must cancel its playbacks.");
        TimbrePlaybackResult mainResult = await fromMain.Completion.WaitAsync(Timeout);
        Require(mainResult.State == TimbrePlaybackState.Completed, "The other window's playback must continue to completion.");
        Require(output.GetDiagnostics().OpenCount == opens && output.GetDiagnostics().CloseCount == 0, "Closing a window touched the shared output.");

        TimbrePlayback first = main.Timbre.Play(Tone(48000, 349));
        TimbrePlayback second = main.Timbre.Play(new TimbreSound(TimbreSource.FromFile(tone)));
        await WaitForTapGrowthAsync(4800);
        first.Cancel();
        TimbrePlaybackResult secondResult = await second.Completion.WaitAsync(Timeout);
        Require(secondResult.State == TimbrePlaybackState.Completed, "Canceling one queued voice cut the other.");
        Record("two-windows+queued-voices", ("secondary", fromSecondary.State), ("main", mainResult.State), ("survivor", secondResult.State));
    }

    private async Task DeviceRemovalFailsWithoutReconnectingAsync(TimbreScope sounds, string tone)
    {
        int opens = output.GetDiagnostics().OpenCount;
        TimbrePlayback playback = sounds.Play(Tone(480000, 196));
        await WaitForTapGrowthAsync(4800);
        SDL.Event removed = default;
        removed.ADevice.Type = SDL.EventType.AudioDeviceRemoved;
        removed.ADevice.Which = output.GetDiagnostics().Device;
        Require(SDL.PushEvent(ref removed), "Could not publish the device removal event.");

        TimbrePlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        Require(result.State == TimbrePlaybackState.Failed && result.Error?.Kind == TimbreErrorKind.DeviceUnavailable,
            $"Device removal: expected Failed/DeviceUnavailable, got {result.State}/{result.Error?.Kind}.");
        await WaitUntilAsync(() => !output.GetDiagnostics().IsOpen);
        Require(output.GetDiagnostics().OpenCount == opens, "The output reconnected on its own.");

        TimbrePlaybackResult replay = await sounds.Play(new TimbreSound(TimbreSource.FromFile(tone))).Completion.WaitAsync(Timeout);
        Require(replay.State == TimbrePlaybackState.Completed && output.GetDiagnostics().OpenCount == opens + 1,
            "An explicit Play after the loss must open the device again.");
        Record("device/removed-then-explicit-play", ("failed", result.State), ("replay", replay.State));
    }

    private async Task<float[]> RenderOfflineAsync(TimbreSound clip, Action<TimbreStartOptions>? configure)
    {
        RecordingSink sink = new();
        using TimbreRuntime offline = new(new TimbreRuntimeOptions { Output = sink });
        TimbrePlaybackResult result = await offline.CreateScope().Play(clip, configure).Completion.WaitAsync(Timeout);
        Require(result.State == TimbrePlaybackState.Completed, $"Deterministic sink: {result.State} ({result.Error?.Message}).");
        return sink.ToArray();
    }

    private async Task WaitForRequestsAsync(long count)
    {
        long target = output.GetDiagnostics().Requests + count;
        await WaitUntilAsync(() => output.GetDiagnostics().Requests >= target);
    }

    private async Task WaitForTapGrowthAsync(long frames)
    {
        long target = tap.SampleCount + (frames * 2);
        await WaitUntilAsync(() => tap.SampleCount >= target);
    }

    // Polls an observable counter; the counter, not elapsed time, is the evidence.
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > Timeout)
            {
                throw new TimeoutException("A Timbre smoke counter did not advance in time.");
            }

            await Task.Delay(5);
        }
    }

    private static async Task<bool> IsCanceledAsync(Task task)
    {
        try
        {
            await task.WaitAsync(Timeout);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }

    private void Record(string name, params (string Key, object? Value)[] values)
    {
        Dictionary<string, object?> entry = new() { ["scenario"] = name };
        foreach ((string key, object? value) in values)
        {
            entry[key] = value is Enum ? value.ToString() : value;
        }

        results.Add(entry);
    }

    private void WriteDiagnostics(Exception? failure)
    {
        SdlTimbreOutputDiagnostics device = output.GetDiagnostics();
        var engine = runtime.GetDiagnostics();
        Dictionary<string, object?> report = new()
        {
            ["host"] = $"{Environment.OSVersion} {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}",
            ["sdl"] = SDL.GetRevision(),
            ["driver"] = SDL.GetCurrentAudioDriver(),
            ["passed"] = failure is null,
            ["failure"] = failure?.ToString(),
            ["scenarios"] = results,
            ["output"] = new
            {
                device.OpenCount,
                device.CloseCount,
                device.IsOpen,
                device.Device,
                DeviceFormat = $"0x{device.DeviceFormat.Format:X}/{device.DeviceFormat.Channels}/{device.DeviceFormat.Frequency}",
                device.DeviceSampleFrames,
                device.Requests,
                device.RequestedBytes,
                device.StarvedRequests,
                device.StarvedBytes,
                device.FramesSubmitted,
                device.QueueHighWaterFrames,
                device.PutFailures,
                device.Flushes,
                device.DevicesLost
            },
            ["engine"] = new
            {
                engine.PlaybacksStarted,
                engine.PlaybacksCompleted,
                engine.PlaybacksCanceled,
                engine.PlaybacksFailed,
                engine.FramesSubmitted,
                engine.FramesConsumed,
                engine.UnderrunFrames,
                engine.ClippedSamples,
                engine.LoopWraps,
                engine.SeeksCompleted,
                engine.SeeksSuperseded,
                engine.OutputOpenCount
            },
            ["tapFrames"] = tap.SampleCount / 2,
            ["lateCallbacks"] = NativeSdlAudioApi.LateCallbacks,
            ["handlerFailures"] = NativeSdlAudioApi.HandlerFailures
        };
        File.WriteAllText(
            Path.Combine(options.ArtifactDirectory, "timbre-diagnostics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static int FirstDifference(float[] actual, float[] expected)
    {
        int length = Math.Min(actual.Length, expected.Length);
        for (int index = 0; index < length; index++)
        {
            if (BitConverter.SingleToInt32Bits(actual[index]) != BitConverter.SingleToInt32Bits(expected[index]))
            {
                return index;
            }
        }

        return actual.Length == expected.Length ? -1 : length;
    }

    private static TimbreSound Tone(int frames, double frequency) =>
        new(TimbreSource.FromReader(() => new ToneReader(frames, frequency, null), $"tone-{frequency}-{frames}"));

    // Synthetic 16-bit stereo RIFF/WAVE tone written by the test application.
    internal static string WriteWav(string path, int sampleRate, double seconds, double frequency)
    {
        int frames = (int)(sampleRate * seconds);
        byte[] bytes = new byte[44 + (frames * 4)];
        Span<byte> span = bytes;
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], bytes.Length - 8);
        "WAVEfmt "u8.CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], 2);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], sampleRate * 4);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], 4);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], 16);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], frames * 4);
        for (int frame = 0; frame < frames; frame++)
        {
            short value = (short)Math.Round(0.2 * short.MaxValue * Math.Sin(2 * Math.PI * frequency * frame / sampleRate));
            BinaryPrimitives.WriteInt16LittleEndian(span[(44 + (frame * 4))..], value);
            BinaryPrimitives.WriteInt16LittleEndian(span[(46 + (frame * 4))..], (short)-value);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private sealed class ToneReader(int frames, double frequency, Task? gate) : TimbreReader
    {
        private long position;

        public override long? LengthFrames => frames;

        public override async ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            if (gate is not null)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            return Fill(destination.Span);
        }

        private TimbreReadResult Fill(Span<float> span)
        {
            int count = (int)Math.Min(span.Length / 2, frames - position);
            for (int index = 0; index < count; index++)
            {
                float value = 0.1f * (float)Math.Sin(2 * Math.PI * frequency * (position + index) / 48000);
                span[index * 2] = value;
                span[(index * 2) + 1] = value;
            }

            position += count;
            return new TimbreReadResult(count, position == frames);
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}

// Application-owned tap in front of the platform output: records exactly the
// PCM the output accepted, in submission order.
internal sealed class PcmTapOutput(ITimbreOutput inner) : ITimbreOutput
{
    private readonly object sync = new();
    private float[] samples = new float[48000 * 2 * 120];
    private long count;

    public ITimbreOutput Inner => inner;

    public long SampleCount
    {
        get
        {
            lock (sync)
            {
                return count;
            }
        }
    }

    public int QueuedFrames => inner.QueuedFrames;

    public void Open(ITimbreOutputClient client) => inner.Open(client);

    public void Submit(ReadOnlySpan<float> block)
    {
        inner.Submit(block);
        lock (sync)
        {
            if (count + block.Length > samples.Length)
            {
                Array.Resize(ref samples, samples.Length * 2);
            }

            block.CopyTo(samples.AsSpan((int)count));
            count += block.Length;
        }
    }

    public void Close() => inner.Close();

    public float[] Copy(long start, long end)
    {
        lock (sync)
        {
            return samples.AsSpan((int)start, (int)(end - start)).ToArray();
        }
    }
}

// Deterministic sink: always empty, so the engine renders as fast as it can.
internal sealed class RecordingSink : ITimbreOutput
{
    private readonly List<float> samples = [];

    public int QueuedFrames => 0;

    public void Open(ITimbreOutputClient client)
    {
    }

    public void Submit(ReadOnlySpan<float> block)
    {
        lock (samples)
        {
            foreach (float sample in block)
            {
                samples.Add(sample);
            }
        }
    }

    public void Close()
    {
    }

    public float[] ToArray()
    {
        lock (samples)
        {
            return samples.ToArray();
        }
    }
}
