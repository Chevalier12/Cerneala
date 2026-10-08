using System.Diagnostics;
using System.Text.Json;
using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Servo;
using Cerneala.UI.Timbre;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.SdlGpuSmoke;

// Native audio Motion smoke: `.timbre.` animations declared in
// TimbreMotionPanel.crn, triggered only by Servo input routed through the real
// SDL window. The PCM tap in front of the platform output is compared block by
// block with the same clip rendered statically at Volume 1 in a deterministic
// sink: Volume is a post-chain gain published on block boundaries, so each
// block's least-squares gain is the Volume that block was mixed with.
internal sealed class TimbreMotionSmoke
{
    private const int Block = 480;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly SmokeOptions options;
    private readonly PcmTapOutput tap;
    private readonly SdlTimbreOutput output;
    private readonly TimbreRuntime runtime;
    private readonly List<TimbrePlayback> accepted = [];
    private readonly List<Dictionary<string, object?>> results = [];

    private TimbreMotionSmoke(SmokeOptions options, PcmTapOutput tap, TimbreRuntime runtime)
    {
        this.options = options;
        this.tap = tap;
        output = (SdlTimbreOutput)tap.Inner;
        this.runtime = runtime;
    }

    private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "timbre");

    public static async Task RunAsync(SmokeOptions options, MainWindow main)
    {
        Application application = Application.Current ?? throw new InvalidOperationException("No Application.");
        PcmTapOutput tap = TimbreSmoke.InstalledTap ?? throw new InvalidOperationException("The PCM tap was not installed at startup.");
        TimbreMotionSmoke smoke = new(options, tap, application.TimbreRuntime);
        application.TimbreRuntime.PlaybackAccepted = playback =>
        {
            lock (smoke.accepted)
            {
                smoke.accepted.Add(playback);
            }
        };
        try
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            await smoke.RunScenariosAsync();
            smoke.WriteDiagnostics(failure: null);
            Console.WriteLine($"SDL_GPU_SMOKE_OK mode=timbre-motion scenarios={smoke.results.Count} opens={smoke.output.GetDiagnostics().OpenCount}");
            main.Close();
        }
        catch (Exception exception)
        {
            smoke.WriteDiagnostics(exception);
            await File.WriteAllTextAsync(Path.Combine(options.ArtifactDirectory, "timbre-motion.error.txt"), exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL timbre-motion: {exception}");
            application.Shutdown(2);
        }
        finally
        {
            application.TimbreRuntime.PlaybackAccepted = null;
        }
    }

    private async Task RunScenariosAsync()
    {
        string tone = TimbreSmoke.WriteWav(Path.Combine(Fixtures, "motion-tone-44100.wav"), 44100, 1.0, 330);
        Window window = new()
        {
            Title = "Cerneala Timbre Motion smoke",
            Width = 680,
            Height = 220,
            Left = 40,
            Top = 40,
            Content = new TimbreMotionPanel()
        };
        window.Show();
        ServoApi servo = new(window);
        _ = await servo.FindAsync(ServoTarget.ById("fade-wav"));
        var declared = runtime.GetDiagnostics();
        Require(declared.PlaybacksStarted == 0 && declared.OutputOpenCount == 0, "Declaring audio Motion started or opened something.");

        (string Id, string Path)[] formats =
        [
            ("fade-wav", tone),
            ("fade-mp3", Path.Combine(Fixtures, "mp3-mpeg1-44100-stereo-cbr128.mp3")),
            ("fade-vorbis", Path.Combine(Fixtures, "vorbis-48000-stereo-q2.ogg")),
            ("fade-opus", Path.Combine(Fixtures, "opus-stereo-48000-96k.opus"))
        ];
        foreach ((string id, string path) in formats)
        {
            await FadeAsync(servo, id, path);
        }

        await FilteredSweepAsync(servo, tone);
        await StreamingTransportAsync(servo);
        await LoopAndHoverAsync(servo);

        await runtime.SyncAsync().WaitAsync(Timeout);
        var final = runtime.GetDiagnostics();
        int targets = TimbrePlaybackMotion.ActiveTargets(((UIElement)window.Content!).Root!);
        Require(final.ActiveVoices == 0 && final.LiveReaders == 0 && final.PlaybacksFailed == 0 && final.MotionSamplesRejected == 0,
            "Audio Motion left voices or readers behind, failed, or rejected samples.");
        Require(targets == 0, $"{targets} audio Motion targets stayed registered.");
        Record("final/baseline", ("activeVoices", final.ActiveVoices), ("liveReaders", final.LiveReaders), ("motionTargets", targets));
        window.Close();
    }

    // Click → @play (sound Volume 0.2) + Tween(400 ms) to 0.8 on the captured
    // playback. Every block's gain lies in [0.2, 0.8], never decreases, starts
    // at 0.2 (time starts at the first PCM) and ends at 0.8.
    private async Task FadeAsync(ServoApi servo, string id, string path)
    {
        var before = runtime.GetDiagnostics();
        long start = tap.SampleCount;
        await servo.ClickAsync(ServoTarget.ById(id));
        TimbrePlayback playback = await LatestAcceptedAsync(before.PlaybacksStarted);
        await WaitUntilAsync(() => playback.State == TimbrePlaybackState.Completed);
        var after = runtime.GetDiagnostics();
        float[] actual = tap.Copy(start, tap.SampleCount);
        float[] reference = await RenderOfflineAsync(new TimbreSound(TimbreSource.FromFile(path), loading: TimbreLoading.Preload));

        Require(actual.Length == reference.Length, $"{id}: tapped {actual.Length} samples, static render {reference.Length}.");
        Require(after.UnderrunFrames == before.UnderrunFrames, $"{id}: underrun frames were padded.");
        double[] gains = BlockGains(actual, reference);
        Require(Math.Abs(gains[0] - 0.2) < 1e-3, $"{id}: first block gain {gains[0]} is not the start Volume 0.2.");
        Require(Math.Abs(gains[^1] - 0.8) < 1e-3, $"{id}: last block gain {gains[^1]} is not the animated Volume 0.8.");
        for (int index = 1; index < gains.Length; index++)
        {
            Require(gains[index] >= gains[index - 1] - 1e-4 && gains[index] <= 0.8 + 1e-3,
                $"{id}: block gain {gains[index]} after {gains[index - 1]} is not a monotonic fade within 0.2–0.8.");
        }

        Require(playback.AnimatedPublications >= 2, $"{id}: only {playback.AnimatedPublications} animated publications.");
        Record($"fade/{id}",
            ("blocks", gains.Length),
            ("rampBlocks", gains.Count(gain => gain > 0.2 + 1e-3 && gain < 0.8 - 1e-3)),
            ("firstGain", gains[0]),
            ("lastGain", gains[^1]),
            ("publications", playback.AnimatedPublications));
    }

    // Cutoff and echo mix animate on the captured playback: the first block is
    // mixed with the declared values (bit-identical to the static render),
    // later blocks differ.
    private async Task FilteredSweepAsync(ServoApi servo, string tone)
    {
        var before = runtime.GetDiagnostics();
        long start = tap.SampleCount;
        await servo.ClickAsync(ServoTarget.ById("sweep-filtered"));
        TimbrePlayback playback = await LatestAcceptedAsync(before.PlaybacksStarted);
        await WaitUntilAsync(() => playback.State == TimbrePlaybackState.Completed);
        float[] actual = tap.Copy(start, tap.SampleCount);
        TimbreParameter<float> cutoff = new("ToneCutoff", 4000f);
        TimbreParameter<float> mix = new("EchoMix", 0.1f);
        float[] reference = await RenderOfflineAsync(new TimbreSound(
            TimbreSource.FromFile(tone),
            loading: TimbreLoading.Preload,
            parameters: [cutoff, mix],
            modifiers: [new LowPass(cutoff), new Delay(time: 0.06f, feedback: 0.3f, mix: mix)]));

        bool firstBlockIdentical = actual.AsSpan(0, Block * 2).SequenceEqual(reference.AsSpan(0, Block * 2));
        int changed = Enumerable.Range(0, Math.Min(actual.Length, reference.Length) / (Block * 2))
            .Count(block => !actual.AsSpan(block * Block * 2, Block * 2).SequenceEqual(reference.AsSpan(block * Block * 2, Block * 2)));
        Require(firstBlockIdentical, "sweep: the first block was not mixed with the declared parameter values.");
        Require(changed > 10, $"sweep: only {changed} blocks differ from the static render.");
        Require(playback.GetMotionSlotValue(1) == 200f && playback.GetMotionSlotValue(2) == 0.6f, "sweep: final parameter values were not held.");
        Record("sweep/filtered",
            ("changedBlocks", changed),
            ("finalCutoff", playback.GetMotionSlotValue(1)),
            ("finalEchoMix", playback.GetMotionSlotValue(2)),
            ("publications", playback.AnimatedPublications));
    }

    // Reactive streaming playback with a 3 s fade: Pause freezes the fade and
    // its publications, Resume continues it, Seek does not restart it and Stop
    // cancels it mid-flight with no further publications.
    private async Task StreamingTransportAsync(ServoApi servo)
    {
        var before = runtime.GetDiagnostics();
        await servo.ClickAsync(ServoTarget.ById("transport-play"));
        TimbrePlayback music = await LatestAcceptedAsync(before.PlaybacksStarted);
        await WaitUntilAsync(() => music.Volume > 0.15f);
        var playing = runtime.GetDiagnostics();
        Require(playing.StreamingBufferBytes > 0, "The 2.4 MB MP3 did not stream under Auto loading.");

        await servo.ClickAsync(ServoTarget.ById("transport-pause"));
        await WaitUntilAsync(() => music.State == TimbrePlaybackState.Paused);
        await runtime.SyncAsync().WaitAsync(Timeout);
        await WaitForRequestsAsync(5);
        float pausedVolume = music.Volume;
        long pausedPublications = music.AnimatedPublications;
        long pausedTap = tap.SampleCount;
        await WaitForRequestsAsync(30);
        Require(music.Volume == pausedVolume && music.AnimatedPublications == pausedPublications && tap.SampleCount == pausedTap,
            "Pause did not freeze the audio animation and the PCM.");

        await servo.ClickAsync(ServoTarget.ById("transport-pause"));
        await WaitUntilAsync(() => music.Volume > pausedVolume + 0.02f);
        float resumedVolume = music.Volume;

        await servo.ClickAsync(ServoTarget.ById("transport-seek"));
        await WaitUntilAsync(() => runtime.GetDiagnostics().SeeksCompleted > before.SeeksCompleted);
        await WaitUntilAsync(() => music.Volume > resumedVolume + 0.02f);
        float seekedVolume = music.Volume;
        Require(seekedVolume < 0.9f, "The fade ended before the mid-flight cancel.");

        await servo.ClickAsync(ServoTarget.ById("transport-stop"));
        await WaitUntilAsync(() => music.State == TimbrePlaybackState.Canceled);
        long canceledPublications = music.AnimatedPublications;
        float canceledVolume = music.Volume;
        await WaitForRequestsAsync(20);
        Require(music.AnimatedPublications == canceledPublications && music.Volume == canceledVolume,
            "The canceled playback still received animated values.");
        var after = runtime.GetDiagnostics();
        Require(after.PlaybacksStarted - before.PlaybacksStarted == 1, "The transport actions started extra playbacks.");
        Record("reactive/streaming-transport-fade",
            ("pausedVolume", pausedVolume),
            ("resumedVolume", resumedVolume),
            ("seekedVolume", seekedVolume),
            ("canceledVolume", canceledVolume),
            ("publications", canceledPublications),
            ("streamingBufferBytes", playing.StreamingBufferBytes));
    }

    // Looping playback with a 600 ms fade: the fade completes once and is not
    // restarted by wraps; hovering the transport area animates the captured
    // looper to 0.3.
    private async Task LoopAndHoverAsync(ServoApi servo)
    {
        var before = runtime.GetDiagnostics();
        await servo.ClickAsync(ServoTarget.ById("loop-play"));
        TimbrePlayback looper = await LatestAcceptedAsync(before.PlaybacksStarted);
        await WaitUntilAsync(() => looper.Volume == 0.8f);
        long completedPublications = looper.AnimatedPublications;
        await WaitUntilAsync(() => runtime.GetDiagnostics().LoopWraps >= before.LoopWraps + 2);
        Require(looper.Volume == 0.8f && looper.AnimatedPublications == completedPublications,
            "A loop wrap restarted or extended the completed fade.");

        await servo.HoverAsync(ServoTarget.ById("transport-area"));
        await WaitUntilAsync(() => looper.Volume == 0.3f);
        Require(runtime.GetDiagnostics().PlaybacksStarted == before.PlaybacksStarted + 1, "Hover started a playback.");

        await servo.ClickAsync(ServoTarget.ById("loop-stop"));
        await WaitUntilAsync(() => looper.State == TimbrePlaybackState.Canceled);
        Record("reactive/loop-fade-and-hover",
            ("wraps", runtime.GetDiagnostics().LoopWraps - before.LoopWraps),
            ("fadePublications", completedPublications),
            ("hoverVolume", looper.Volume));
    }

    private async Task<TimbrePlayback> LatestAcceptedAsync(long startedBefore)
    {
        await WaitUntilAsync(() => runtime.GetDiagnostics().PlaybacksStarted > startedBefore);
        lock (accepted)
        {
            return accepted[^1];
        }
    }

    // Least-squares gain of each tapped block against the static render.
    private static double[] BlockGains(float[] actual, float[] reference)
    {
        List<double> gains = [];
        for (int offset = 0; offset + (Block * 2) <= reference.Length; offset += Block * 2)
        {
            double cross = 0;
            double energy = 0;
            for (int index = offset; index < offset + (Block * 2); index++)
            {
                cross += actual[index] * (double)reference[index];
                energy += reference[index] * (double)reference[index];
            }

            if (energy > 1e-6)
            {
                gains.Add(cross / energy);
            }
        }

        return gains.ToArray();
    }

    private static async Task<float[]> RenderOfflineAsync(TimbreSound clip)
    {
        RecordingSink sink = new();
        using TimbreRuntime offline = new(new TimbreRuntimeOptions { Output = sink });
        TimbrePlaybackResult result = await offline.CreateScope().Play(clip).Completion.WaitAsync(Timeout);
        Require(result.State == TimbrePlaybackState.Completed, $"Deterministic sink: {result.State} ({result.Error?.Message}).");
        return sink.ToArray();
    }

    private async Task WaitForRequestsAsync(long count)
    {
        long target = output.GetDiagnostics().Requests + count;
        await WaitUntilAsync(() => output.GetDiagnostics().Requests >= target);
    }

    // Polls an observable counter; the counter, not elapsed time, is the evidence.
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > Timeout)
            {
                throw new TimeoutException("A Timbre Motion smoke condition did not hold in time.");
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
        SdlTimbreOutputDiagnostics device = output.GetDiagnostics();
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
                DeviceFormat = $"0x{device.DeviceFormat.Format:X}/{device.DeviceFormat.Channels}/{device.DeviceFormat.Frequency}",
                device.Requests,
                device.StarvedRequests,
                device.QueueHighWaterFrames
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
                engine.AnimatedPublications,
                engine.MotionSamplesRejected,
                engine.ActiveVoices,
                engine.LiveReaders
            },
            ["tapFrames"] = tap.SampleCount / 2
        };
        File.WriteAllText(
            Path.Combine(options.ArtifactDirectory, "timbre-motion-diagnostics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
