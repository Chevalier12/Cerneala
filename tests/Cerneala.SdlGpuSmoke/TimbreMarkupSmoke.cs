using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.SdlGpuSmoke;

// Native Timbre smoke for the .crn path. Every playback is declared in
// TimbreMarkupPanel.crn and started by Servo input routed through the real SDL
// window; nothing here calls the C# sound API to play. The PCM tap in front of
// the platform output is compared with the same clip defined in C# and rendered
// into a deterministic sink, and the engine/output counters are the probes. The
// tap certifies the pipeline up to the output, not what the device played.
internal sealed class TimbreMarkupSmoke
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly SmokeOptions options;
    private readonly PcmTapOutput tap;
    private readonly SdlSoundOutput output;
    private readonly SoundRuntime runtime;
    private readonly List<Dictionary<string, object?>> results = [];

    private TimbreMarkupSmoke(SmokeOptions options, PcmTapOutput tap, SoundRuntime runtime)
    {
        this.options = options;
        this.tap = tap;
        output = (SdlSoundOutput)tap.Inner;
        this.runtime = runtime;
    }

    private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "timbre");

    public static async Task RunAsync(SmokeOptions options, MainWindow main)
    {
        Application application = Application.Current ?? throw new InvalidOperationException("No Application.");
        PcmTapOutput tap = TimbreSmoke.InstalledTap ?? throw new InvalidOperationException("The PCM tap was not installed at startup.");
        TimbreMarkupSmoke smoke = new(options, tap, application.SoundRuntime);
        try
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            await smoke.RunScenariosAsync();
            smoke.WriteDiagnostics(failure: null);
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=timbre-markup scenarios={smoke.results.Count} " +
                $"device={smoke.output.GetDiagnostics().DeviceFormat} opens={smoke.output.GetDiagnostics().OpenCount}");
            main.Close();
        }
        catch (Exception exception)
        {
            smoke.WriteDiagnostics(exception);
            await File.WriteAllTextAsync(Path.Combine(options.ArtifactDirectory, "timbre-markup.error.txt"), exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL timbre-markup: {exception}");
            application.Shutdown(2);
        }
    }

    private async Task RunScenariosAsync()
    {
        // The markup clip's Source is resolved against the application base
        // directory when it first plays, so the tone may be written after the
        // window is built.
        string tone = TimbreSmoke.WriteWav(Path.Combine(Fixtures, "markup-tone-44100.wav"), 44100, 0.5, 330);

        Window primary = OpenPanelWindow("Cerneala Timbre markup smoke", 40);
        ServoApi servo = new(primary);
        _ = await servo.FindAsync(ServoTarget.ById("play-wav"));
        var declared = runtime.GetDiagnostics();
        Require(declared.PlaybacksStarted == 0 && declared.OutputOpenCount == 0 && declared.CacheEntries == 0,
            "Declaring SoundClips and Aspects started, opened or loaded something.");
        Record("declaration/no-playback-or-io",
            ("started", declared.PlaybacksStarted), ("outputOpens", declared.OutputOpenCount), ("cacheEntries", declared.CacheEntries));

        (string Id, string Path)[] formats =
        [
            ("play-wav", tone),
            ("play-mp3", Path.Combine(Fixtures, "mp3-mpeg1-44100-stereo-cbr128.mp3")),
            ("play-vorbis", Path.Combine(Fixtures, "vorbis-48000-stereo-q2.ogg")),
            ("play-opus", Path.Combine(Fixtures, "opus-stereo-48000-96k.opus"))
        ];
        foreach ((string id, string path) in formats)
        {
            await CompareClickAsync(servo, id, new SoundClip(SoundSource.FromFile(path), loading: SoundLoading.Preload), null);
        }

        SoundParameter<float> cutoff = new("ToneCutoff", 1200f);
        await CompareClickAsync(
            servo,
            "play-filtered",
            new SoundClip(
                SoundSource.FromFile(tone),
                volume: 0.8f,
                loading: SoundLoading.Preload,
                parameters: [cutoff],
                modifiers: [new LowPass(cutoff), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]),
            start => start.Set(cutoff, 800f));

        await OverlapAsync(servo);
        await StreamingTransportAsync(servo);
        await ReactiveLoopAsync(servo);
        await TwoWindowsAsync(servo, primary);
        primary.Close();
    }

    private async Task CompareClickAsync(ServoApi servo, string id, SoundClip oracle, Action<SoundStartOptions>? configure)
    {
        var before = runtime.GetDiagnostics();
        long start = tap.SampleCount;
        Stopwatch watch = Stopwatch.StartNew();
        await servo.ClickAsync(ServoTarget.ById(id));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCompleted > before.PlaybacksCompleted);
        watch.Stop();
        var after = runtime.GetDiagnostics();
        float[] actual = tap.Copy(start, tap.SampleCount);
        float[] expected = await RenderOfflineAsync(oracle, configure);

        Require(after.PlaybacksStarted - before.PlaybacksStarted == 1, $"{id}: one click started {after.PlaybacksStarted - before.PlaybacksStarted} playbacks.");
        Require(after.PlaybacksFailed == before.PlaybacksFailed, $"{id}: the playback failed.");
        Require(after.UnderrunFrames == before.UnderrunFrames, $"{id}: underrun frames were padded.");
        int firstDifference = FirstDifference(actual, expected);
        Require(
            actual.Length == expected.Length && firstDifference < 0,
            $"{id}: tapped PCM ({actual.Length} samples) differs from the C# clip in the deterministic sink ({expected.Length}) at sample {firstDifference}.");
        Record($"click/{id}",
            ("frames", actual.Length / 2),
            ("wallMs", watch.Elapsed.TotalMilliseconds),
            ("sha256", Convert.ToHexString(SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(actual.AsSpan())))),
            ("matchesCSharpClip", true));
    }

    private async Task OverlapAsync(ServoApi servo)
    {
        var before = runtime.GetDiagnostics();
        await servo.ClickAsync(ServoTarget.ById("play-mp3"));
        await servo.ClickAsync(ServoTarget.ById("play-mp3"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCompleted >= before.PlaybacksCompleted + 2);
        var after = runtime.GetDiagnostics();
        Require(after.PlaybacksStarted - before.PlaybacksStarted == 2 && after.PlaybacksCanceled == before.PlaybacksCanceled,
            "Two clicks without a handle must overlap, not replace each other.");
        Record("click/overlap", ("started", after.PlaybacksStarted - before.PlaybacksStarted), ("completed", after.PlaybacksCompleted - before.PlaybacksCompleted));
    }

    private async Task StreamingTransportAsync(ServoApi servo)
    {
        var before = runtime.GetDiagnostics();
        await servo.ClickAsync(ServoTarget.ById("transport-play"));
        await WaitForTapGrowthAsync(9600);
        var playing = runtime.GetDiagnostics();
        Require(playing.PlaybacksStarted - before.PlaybacksStarted == 1, "Checking Play did not start exactly one playback.");
        Require(playing.StreamingBufferBytes > 0, "The 2.4 MB MP3 did not stream under Auto loading.");

        await servo.ClickAsync(ServoTarget.ById("transport-pause"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PausesApplied > before.PausesApplied);
        await runtime.SyncAsync().WaitAsync(Timeout);
        long paused = tap.SampleCount;
        await WaitForRequestsAsync(30);
        Require(tap.SampleCount == paused, "The paused markup playback still produced PCM.");

        await servo.ClickAsync(ServoTarget.ById("transport-pause"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().ResumesApplied > before.ResumesApplied);
        await WaitForTapGrowthAsync(4800);

        await servo.ClickAsync(ServoTarget.ById("transport-seek"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().SeeksCompleted > before.SeeksCompleted);
        await WaitForTapGrowthAsync(4800);

        await servo.ClickAsync(ServoTarget.ById("transport-stop"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCanceled > before.PlaybacksCanceled);
        var after = runtime.GetDiagnostics();
        Require(after.PlaybacksStarted == playing.PlaybacksStarted, "The transport actions started extra playbacks.");
        Record("reactive/streaming-transport",
            ("pauses", after.PausesApplied - before.PausesApplied),
            ("resumes", after.ResumesApplied - before.ResumesApplied),
            ("seeks", after.SeeksCompleted - before.SeeksCompleted),
            ("canceled", after.PlaybacksCanceled - before.PlaybacksCanceled),
            ("streamingBufferBytes", playing.StreamingBufferBytes));
    }

    private async Task ReactiveLoopAsync(ServoApi servo)
    {
        var before = runtime.GetDiagnostics();
        await servo.ClickAsync(ServoTarget.ById("loop-play"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().LoopWraps >= before.LoopWraps + 2);

        // true → false neither stops nor restarts the loop.
        await servo.ClickAsync(ServoTarget.ById("loop-play"));
        await WaitForTapGrowthAsync(4800);
        var unchecked_ = runtime.GetDiagnostics();
        Require(unchecked_.PlaybacksStarted - before.PlaybacksStarted == 1 && unchecked_.PlaybacksCanceled == before.PlaybacksCanceled,
            "Unchecking the reactive source stopped or restarted the loop.");

        await servo.ClickAsync(ServoTarget.ById("loop-stop"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCanceled > before.PlaybacksCanceled);

        // false → true activates again.
        await servo.ClickAsync(ServoTarget.ById("loop-play"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksStarted > unchecked_.PlaybacksStarted);
        await servo.ClickAsync(ServoTarget.ById("loop-stop"));
        await servo.ClickAsync(ServoTarget.ById("loop-stop"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCanceled >= before.PlaybacksCanceled + 2);
        var after = runtime.GetDiagnostics();
        Require(after.PlaybacksStarted - before.PlaybacksStarted == 2, "The reactive loop did not start exactly once per activation.");
        Record("reactive/loop",
            ("wraps", after.LoopWraps - before.LoopWraps),
            ("started", after.PlaybacksStarted - before.PlaybacksStarted),
            ("canceled", after.PlaybacksCanceled - before.PlaybacksCanceled));
    }

    private async Task TwoWindowsAsync(ServoApi primaryServo, Window primary)
    {
        Window secondary = OpenPanelWindow("Cerneala Timbre markup smoke secondary", 120);
        ServoApi secondaryServo = new(secondary);
        var before = runtime.GetDiagnostics();
        await secondaryServo.ClickAsync(ServoTarget.ById("transport-play"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksStarted > before.PlaybacksStarted);
        await WaitForTapGrowthAsync(4800);
        int opens = output.GetDiagnostics().OpenCount;
        await primaryServo.ClickAsync(ServoTarget.ById("play-wav"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksStarted >= before.PlaybacksStarted + 2);

        secondary.Close();
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCanceled > before.PlaybacksCanceled);
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksCompleted > before.PlaybacksCompleted);
        var after = runtime.GetDiagnostics();
        Require(after.PlaybacksCanceled - before.PlaybacksCanceled == 1, "Closing a window must cancel only its own playback.");
        Require(output.GetDiagnostics().OpenCount == opens && output.GetDiagnostics().CloseCount == 0, "Closing a window touched the shared output.");
        Require(!primary.IsClosed, "The primary window closed with the secondary one.");
        Record("multiwindow/close-cancels-local",
            ("canceled", after.PlaybacksCanceled - before.PlaybacksCanceled),
            ("completed", after.PlaybacksCompleted - before.PlaybacksCompleted),
            ("outputOpens", output.GetDiagnostics().OpenCount));
    }

    private static Window OpenPanelWindow(string title, int offset)
    {
        Window window = new()
        {
            Title = title,
            Width = 640,
            Height = 200,
            Left = offset,
            Top = offset,
            Content = new TimbreMarkupPanel()
        };
        window.Show();
        return window;
    }

    private static async Task<float[]> RenderOfflineAsync(SoundClip clip, Action<SoundStartOptions>? configure)
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
                throw new TimeoutException("A Timbre markup smoke counter did not advance in time.");
            }

            await Task.Delay(5);
        }
    }

    private void Record(string name, params (string Key, object? Value)[] values)
    {
        Dictionary<string, object?> entry = new() { ["scenario"] = name };
        foreach ((string key, object? value) in values)
        {
            entry[key] = value;
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
            ["passed"] = failure is null,
            ["failure"] = failure?.ToString(),
            ["scenarios"] = results,
            ["output"] = new
            {
                device.OpenCount,
                device.CloseCount,
                device.IsOpen,
                DeviceFormat = $"0x{device.DeviceFormat.Format:X}/{device.DeviceFormat.Channels}/{device.DeviceFormat.Frequency}",
                device.Requests,
                device.StarvedRequests,
                device.FramesSubmitted,
                device.QueueHighWaterFrames,
                device.DevicesLost
            },
            ["engine"] = new
            {
                engine.PlaybacksStarted,
                engine.PlaybacksCompleted,
                engine.PlaybacksCanceled,
                engine.PlaybacksFailed,
                engine.PausesApplied,
                engine.ResumesApplied,
                engine.SeeksCompleted,
                engine.LoopWraps,
                engine.UnderrunFrames,
                engine.ActiveVoices,
                engine.LiveReaders,
                engine.LiveScopes,
                engine.CacheEntries,
                engine.OutputOpenCount
            },
            ["tapFrames"] = tap.SampleCount / 2
        };
        File.WriteAllText(
            Path.Combine(options.ArtifactDirectory, "timbre-markup-diagnostics.json"),
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
}
