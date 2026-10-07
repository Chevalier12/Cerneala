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
    private readonly SdlSoundOutput output;
    private readonly SoundRuntime runtime;
    private readonly List<Dictionary<string, object?>> results = [];

    private TimbreSmoke(SmokeOptions options, MainWindow main, PcmTapOutput tap, SdlSoundOutput output, SoundRuntime runtime)
    {
        this.options = options;
        this.main = main;
        this.tap = tap;
        this.output = output;
        this.runtime = runtime;
    }

    private static PcmTapOutput? InstalledTap { get; set; }

    private string Fixtures => Path.Combine(AppContext.BaseDirectory, "timbre");

    // From App.OnStartup, before any window takes Application.SoundRuntime.
    public static void Install(Application application)
    {
        ISoundOutput platformOutput = WindowApplicationRuntime.Current?.SoundOutput ??
            throw new InvalidOperationException("The SDL window platform supplies no audio output.");
        InstalledTap = new PcmTapOutput(platformOutput);
        application.SoundRuntime = new SoundRuntime(new SoundRuntimeOptions { Output = InstalledTap });
    }

    public static async Task RunAsync(SmokeOptions options, MainWindow main)
    {
        Application application = Application.Current ?? throw new InvalidOperationException("No Application.");
        PcmTapOutput tap = InstalledTap ?? throw new InvalidOperationException("The PCM tap was not installed at startup.");
        SdlSoundOutput output = (SdlSoundOutput)tap.Inner;
        TimbreSmoke smoke = new(options, main, tap, output, application.SoundRuntime);
        string errorPath = Path.Combine(options.ArtifactDirectory, "timbre.error.txt");
        try
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            await smoke.RunScenariosAsync(application);
            smoke.WriteDiagnostics(failure: null);
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=timbre scenarios={smoke.results.Count} " +
                $"device={output.GetDiagnostics().DeviceFormat} opens={output.GetDiagnostics().OpenCount}");
            application.SoundRuntime.Dispose();
            main.Close();
        }
        catch (Exception exception)
        {
            smoke.WriteDiagnostics(exception);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL timbre: {exception}");
            application.SoundRuntime.Dispose();
            application.Shutdown(2);
        }
    }

    private async Task RunScenariosAsync(Application application)
    {
        SoundScope sounds = application.Sounds;
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
            foreach (SoundLoading loading in new[] { SoundLoading.Auto, SoundLoading.Streaming })
            {
                await CompareAsync(
                    $"{Path.GetFileName(path)}/{loading}",
                    sounds,
                    new SoundClip(SoundSource.FromFile(path), loading: loading),
                    new SoundClip(SoundSource.FromFile(path), loading: SoundLoading.Preload));
            }
        }

        SoundParameter<float> cutoff = new("ToneCutoff", 1200f);
        SoundModifier[] chain = [new LowPass(cutoff), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)];
        await CompareAsync(
            "modified/LowPass+Delay",
            sounds,
            new SoundClip(SoundSource.FromFile(tone), volume: 0.8f, parameters: [cutoff], modifiers: chain),
            new SoundClip(SoundSource.FromFile(tone), volume: 0.8f, loading: SoundLoading.Preload, parameters: [cutoff], modifiers: chain),
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

    private async Task AbsentDeviceFailsWithoutRetryAsync(SoundScope sounds, string tone)
    {
        Require(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, AbsentDriver, SDL.HintPriority.Override), "Could not select the absent audio driver.");
        SoundPlaybackResult failed;
        try
        {
            failed = await sounds.Play(new SoundClip(SoundSource.FromFile(tone))).Completion.WaitAsync(Timeout);
        }
        finally
        {
            SDL.ResetHint(SDL.Hints.AudioDriver);
        }

        Require(failed.State == SoundPlaybackState.Failed, $"Absent device: expected Failed, got {failed.State}.");
        Require(failed.Error?.Kind == SoundErrorKind.DeviceUnavailable, $"Absent device: expected DeviceUnavailable, got {failed.Error?.Kind}.");
        Require(output.GetDiagnostics().OpenCount == 0, "Absent device: an output was opened.");

        // No automatic retry: only an explicit Play opens the default device.
        SoundPlaybackResult explicitRetry = await sounds.Play(new SoundClip(SoundSource.FromFile(tone))).Completion.WaitAsync(Timeout);
        Require(explicitRetry.State == SoundPlaybackState.Completed, $"Explicit retry: expected Completed, got {explicitRetry.State}.");
        Require(output.GetDiagnostics().OpenCount == 1, "Explicit retry did not open the device exactly once.");
        Record("device/absent-then-explicit-play",
            ("failed", failed.State), ("error", failed.Error?.InnerException?.Message), ("retry", explicitRetry.State));
    }

    private async Task CompareAsync(
        string name,
        SoundScope sounds,
        SoundClip clip,
        SoundClip oracleClip,
        Action<SoundStartOptions>? configure = null)
    {
        long underrunsBefore = runtime.GetDiagnostics().UnderrunFrames;
        long start = tap.SampleCount;
        Stopwatch watch = Stopwatch.StartNew();
        SoundPlayback playback = sounds.Play(clip, configure);
        SoundPlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        watch.Stop();
        float[] actual = tap.Copy(start, tap.SampleCount);
        float[] expected = await RenderOfflineAsync(oracleClip, configure);
        long underruns = runtime.GetDiagnostics().UnderrunFrames - underrunsBefore;

        Require(result.State == SoundPlaybackState.Completed, $"{name}: expected Completed, got {result.State} ({result.Error?.Message}).");
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

    private async Task OverlapAsync(SoundScope sounds, string tone)
    {
        SoundClip clip = new(SoundSource.FromFile(tone));
        SoundPlayback first = sounds.Play(clip);
        await WaitForRequestsAsync(5);
        SoundPlayback second = sounds.Play(clip);
        SoundPlaybackResult[] done = await Task.WhenAll(first.Completion, second.Completion).WaitAsync(Timeout);
        Require(done.All(result => result.State == SoundPlaybackState.Completed), "Overlap: both playbacks must complete.");
        Record("overlap", ("first", done[0].State), ("second", done[1].State));
    }

    private async Task ReplacementAndCancelAsync(SoundScope sounds, string tone)
    {
        SoundHandle slot = sounds.CreateHandle();
        SoundPlayback replaced = sounds.Play(Tone(96000, 220), handle: slot);
        await WaitForTapGrowthAsync(4800);
        SoundPlayback replacement = sounds.Play(new SoundClip(SoundSource.FromFile(tone)), handle: slot);
        Require(replaced.State == SoundPlaybackState.Canceled, "Replacement must cancel the previous occupant.");
        Require((await replacement.Completion.WaitAsync(Timeout)).State == SoundPlaybackState.Completed, "Replacement must complete.");

        int opens = output.GetDiagnostics().OpenCount;
        SoundPlayback canceled = sounds.Play(Tone(96000, 247));
        await WaitForTapGrowthAsync(4800);
        canceled.Cancel();
        canceled.Cancel();
        Require((await canceled.Completion).State == SoundPlaybackState.Canceled, "Cancel must end Canceled.");
        Require(output.GetDiagnostics().OpenCount == opens && output.GetDiagnostics().IsOpen, "A local cancel closed the shared output.");
        Record("replacement+cancel", ("replaced", replaced.State), ("replacement", replacement.State), ("canceled", canceled.State));
    }

    private async Task PausePendingAsync(SoundScope sounds)
    {
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SoundPlayback playback = sounds.Play(new SoundClip(
            SoundSource.FromReader(() => new ToneReader(24000, 392, gate.Task), "gated-tone"),
            loading: SoundLoading.Preload));
        Require(playback.State == SoundPlaybackState.Pending, "Gated playback must start Pending.");
        playback.Pause();
        long tapped = tap.SampleCount;
        gate.SetResult();
        await WaitForRequestsAsync(30);
        Require(tap.SampleCount == tapped, "Pause in Pending let PCM through before Resume.");
        Require(playback.State == SoundPlaybackState.Paused, $"Pending pause: expected Paused, got {playback.State}.");

        playback.Resume();
        SoundPlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        Require(result.State == SoundPlaybackState.Completed, "Resumed pending playback must complete.");
        Require(tap.SampleCount - tapped >= 24000 * 2, "Resumed pending playback lost PCM.");
        Record("pause-pending", ("state", result.State));
    }

    private async Task PauseResumeAsync(SoundScope sounds)
    {
        SoundPlayback playback = sounds.Play(Tone(48000, 294));
        await WaitForTapGrowthAsync(9600);
        playback.Pause();
        await runtime.SyncAsync().WaitAsync(Timeout);
        long tapped = tap.SampleCount;
        TimeSpan position = playback.Position;
        await WaitForRequestsAsync(30);
        Require(tap.SampleCount == tapped, "Paused playback still produced PCM.");
        Require(playback.State == SoundPlaybackState.Paused && playback.Position == position, "Pause did not hold the position.");
        playback.Resume();
        playback.Resume();
        SoundPlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        Require(result.State == SoundPlaybackState.Completed, "Resumed playback must complete.");
        Record("pause-resume", ("pausedAtMs", position.TotalMilliseconds), ("state", result.State));
    }

    private async Task SeekStreamingAsync(SoundScope sounds)
    {
        SoundPlayback playback = sounds.Play(new SoundClip(
            SoundSource.FromFile(Path.Combine(Fixtures, "mp3-long-22050-mono-cbr64.mp3")),
            loading: SoundLoading.Streaming));
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

    private async Task LoopAsync(SoundScope sounds, string shortTone)
    {
        long wrapsBefore = runtime.GetDiagnostics().LoopWraps;
        SoundPlayback playback = sounds.Play(new SoundClip(SoundSource.FromFile(shortTone), loop: true));
        await WaitForTapGrowthAsync(12000 * 3);
        Require(playback.State == SoundPlaybackState.Playing, "A looping playback ended on its own.");
        playback.Cancel();
        SoundPlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        long wraps = runtime.GetDiagnostics().LoopWraps - wrapsBefore;
        Require(result.State == SoundPlaybackState.Canceled && wraps >= 2, $"Loop: state {result.State}, wraps {wraps}.");
        Record("loop", ("wraps", wraps), ("state", result.State));
    }

    private async Task TwoWindowsAndQueuedVoicesAsync(string tone)
    {
        Window secondary = new() { Title = "Cerneala Timbre smoke secondary", Width = 360, Height = 220, Left = 60, Top = 60 };
        secondary.Show();
        SoundPlayback fromSecondary = secondary.Sounds.Play(Tone(96000, 262));
        SoundPlayback fromMain = main.Sounds.Play(Tone(48000, 330));
        await WaitForTapGrowthAsync(4800);
        Require(output.GetDiagnostics().QueueHighWaterFrames <= 1920, "The software queue exceeded 40 ms.");
        int opens = output.GetDiagnostics().OpenCount;

        secondary.Close();
        Require(fromSecondary.State == SoundPlaybackState.Canceled, "Closing a window must cancel its playbacks.");
        SoundPlaybackResult mainResult = await fromMain.Completion.WaitAsync(Timeout);
        Require(mainResult.State == SoundPlaybackState.Completed, "The other window's playback must continue to completion.");
        Require(output.GetDiagnostics().OpenCount == opens && output.GetDiagnostics().CloseCount == 0, "Closing a window touched the shared output.");

        SoundPlayback first = main.Sounds.Play(Tone(48000, 349));
        SoundPlayback second = main.Sounds.Play(new SoundClip(SoundSource.FromFile(tone)));
        await WaitForTapGrowthAsync(4800);
        first.Cancel();
        SoundPlaybackResult secondResult = await second.Completion.WaitAsync(Timeout);
        Require(secondResult.State == SoundPlaybackState.Completed, "Canceling one queued voice cut the other.");
        Record("two-windows+queued-voices", ("secondary", fromSecondary.State), ("main", mainResult.State), ("survivor", secondResult.State));
    }

    private async Task DeviceRemovalFailsWithoutReconnectingAsync(SoundScope sounds, string tone)
    {
        int opens = output.GetDiagnostics().OpenCount;
        SoundPlayback playback = sounds.Play(Tone(480000, 196));
        await WaitForTapGrowthAsync(4800);
        SDL.Event removed = default;
        removed.ADevice.Type = SDL.EventType.AudioDeviceRemoved;
        removed.ADevice.Which = output.GetDiagnostics().Device;
        Require(SDL.PushEvent(ref removed), "Could not publish the device removal event.");

        SoundPlaybackResult result = await playback.Completion.WaitAsync(Timeout);
        Require(result.State == SoundPlaybackState.Failed && result.Error?.Kind == SoundErrorKind.DeviceUnavailable,
            $"Device removal: expected Failed/DeviceUnavailable, got {result.State}/{result.Error?.Kind}.");
        await WaitUntilAsync(() => !output.GetDiagnostics().IsOpen);
        Require(output.GetDiagnostics().OpenCount == opens, "The output reconnected on its own.");

        SoundPlaybackResult replay = await sounds.Play(new SoundClip(SoundSource.FromFile(tone))).Completion.WaitAsync(Timeout);
        Require(replay.State == SoundPlaybackState.Completed && output.GetDiagnostics().OpenCount == opens + 1,
            "An explicit Play after the loss must open the device again.");
        Record("device/removed-then-explicit-play", ("failed", result.State), ("replay", replay.State));
    }

    private async Task<float[]> RenderOfflineAsync(SoundClip clip, Action<SoundStartOptions>? configure)
    {
        RecordingSink sink = new();
        using SoundRuntime offline = new(new SoundRuntimeOptions { Output = sink });
        SoundPlaybackResult result = await offline.CreateScope().Play(clip, configure).Completion.WaitAsync(Timeout);
        Require(result.State == SoundPlaybackState.Completed, $"Deterministic sink: {result.State} ({result.Error?.Message}).");
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
        SdlSoundOutputDiagnostics device = output.GetDiagnostics();
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

    private static SoundClip Tone(int frames, double frequency) =>
        new(SoundSource.FromReader(() => new ToneReader(frames, frequency, null), $"tone-{frequency}-{frames}"));

    // Synthetic 16-bit stereo RIFF/WAVE tone written by the test application.
    private static string WriteWav(string path, int sampleRate, double seconds, double frequency)
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

    private sealed class ToneReader(int frames, double frequency, Task? gate) : SoundReader
    {
        private long position;

        public override long? LengthFrames => frames;

        public override async ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            if (gate is not null)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            return Fill(destination.Span);
        }

        private SoundReadResult Fill(Span<float> span)
        {
            int count = (int)Math.Min(span.Length / 2, frames - position);
            for (int index = 0; index < count; index++)
            {
                float value = 0.1f * (float)Math.Sin(2 * Math.PI * frequency * (position + index) / 48000);
                span[index * 2] = value;
                span[(index * 2) + 1] = value;
            }

            position += count;
            return new SoundReadResult(count, position == frames);
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
internal sealed class PcmTapOutput(ISoundOutput inner) : ISoundOutput
{
    private readonly object sync = new();
    private float[] samples = new float[48000 * 2 * 120];
    private long count;

    public ISoundOutput Inner => inner;

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

    public void Open(ISoundOutputClient client) => inner.Open(client);

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
internal sealed class RecordingSink : ISoundOutput
{
    private readonly List<float> samples = [];

    public int QueuedFrames => 0;

    public void Open(ISoundOutputClient client)
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
