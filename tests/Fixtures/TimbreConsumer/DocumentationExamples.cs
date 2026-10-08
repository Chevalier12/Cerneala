using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace TimbreConsumer;

// The code samples of the canonical Timbre API pages, compiled against public
// API only. Each method holds the snippet of one page; parameters stand for the
// variables a page's snippet assumes.
public static class DocumentationExamples
{
    public static void TimbreMotionFacadePage(TimbrePlayback playback, TimbreParameter<float> toneCutoff, MotionSpec<float> transition)
    {
        playback.Motion()
            .Animate(TimbrePlayback.VolumeParameter)
            .To(0.8f)
            .With(transition);

        playback.Motion()
            .Animate(toneCutoff)
            .To(6000f)
            .With(transition);
    }

    public static void TimbreMotionAnimationBuilderPage(TimbrePlayback playback)
    {
        MotionHandle fade = playback.Motion()
            .Animate(TimbrePlayback.VolumeParameter)
            .From(0f)
            .To(1f)
            .With(new TweenSpec<float>(TimeSpan.FromMilliseconds(300), Easings.EaseOut));

        fade.Cancel(); // keeps the last sampled volume
    }
    public static void TimbreClipPage()
    {
        var plainTimbre = new TimbreSound("audio/confirm.wav");

        var toneCutoff = new TimbreParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
        var filteredTimbre = new TimbreSound(
            source: "audio/confirm.wav",
            parameters: [toneCutoff],
            modifiers: [new LowPass(cutoff: toneCutoff)]);

        var echo = new TimbreSound(
            "audio/confirm.wav",
            modifiers: [new LowPass(cutoff: 1200f), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);
        _ = (plainTimbre, filteredTimbre, echo);
    }

    public static void TimbreSourcePage()
    {
        TimbreSource file = "audio/confirm.wav";
        TimbreSource stream = TimbreSource.FromStream(() => File.OpenRead("audio/music.ogg"), name: "music");
        TimbreSource generated = TimbreSource.FromReader(() => new MyToneReader(), name: "tone");
        TimbreSource budgeted = TimbreSource.FromReader(budget =>
        {
            budget.Reserve(MyToneReader.TableBytes);
            return new MyToneReader();
        }, name: "budgeted tone");
        _ = (file, stream, generated, budgeted);
    }

    public static TimbreSource TimbreMemoryBudgetPage()
    {
        const int TableBytes = 256 * 1024;

        TimbreSource source = TimbreSource.FromReader(budget =>
        {
            // Reserve first: an oversized request fails here, before allocating.
            budget.Reserve(TableBytes);
            return new WavetableReader(new float[TableBytes / sizeof(float)]);
        }, name: "wavetable");
        return source;
    }

    public static TimbreSource TimbreMemoryReservationPage()
    {
        TimbreSource source = TimbreSource.FromReader(budget =>
        {
            // Temporary scratch memory needed only while the reader is built.
            using (budget.Reserve(64 * 1024))
            {
                float[] scratch = new float[16 * 1024];
                PrecomputeTable(scratch);
            }

            return new TableReader();
        });
        return source;
    }

    private static void PrecomputeTable(float[] scratch) => scratch.AsSpan().Fill(0.5f);

    private sealed class WavetableReader(float[] table) : TimbreReader
    {
        public override long? LengthFrames => table.Length / TimbreRuntime.ChannelCount;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new TimbreReadResult(0, endOfSource: true));

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class TableReader : TimbreReader
    {
        public override long? LengthFrames => 0;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new TimbreReadResult(0, endOfSource: true));

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    public static TimbreSound TimbreReaderPage() =>
        new(TimbreSource.FromReader(() => new SilenceReader(48000)));

    public static void TimbreReadResultPage()
    {
        var last = new TimbreReadResult(frames: 120, endOfSource: true);
        var waiting = new TimbreReadResult(frames: 0, endOfSource: false);
        _ = (last, waiting);
    }

    public static TimbreSound TimbreLoadingPage() =>
        new("audio/music.ogg", loop: true, loading: TimbreLoading.Streaming);

    public static void TimbreParameterPage(TimbreScope sounds)
    {
        var toneCutoff = new TimbreParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
        var clip = new TimbreSound("audio/confirm.wav", parameters: [toneCutoff], modifiers: [new LowPass(cutoff: toneCutoff)]);

        TimbrePlayback playback = sounds.Play(clip, start => start.Set(toneCutoff, 800f));
        playback.Set(toneCutoff, 6000f);
    }

    public static void TimbreInputPage()
    {
        var toneCutoff = new TimbreParameter<float>("ToneCutoff", 1200f);
        var fixedFilter = new LowPass(cutoff: 1200f);       // constant
        var driven = new LowPass(cutoff: toneCutoff);       // parameter
        _ = (fixedFilter, driven);
    }

    public static void LowPassPage()
    {
        var cutoff = new TimbreParameter<float>("ToneCutoff", 1200f);
        var clip = new TimbreSound("audio/confirm.wav", parameters: [cutoff], modifiers: [new LowPass(cutoff: cutoff)]);
        var fixedFilter = new LowPass(cutoff: 800f);
        _ = (clip, fixedFilter);
    }

    public static TimbreSound DelayPage()
    {
        var echoMix = new TimbreParameter<float>("EchoMix", 0.15f);
        return new TimbreSound(
            "audio/confirm.wav",
            parameters: [echoMix],
            modifiers: [new Delay(time: 0.12f, feedback: 0.20f, mix: echoMix)]);
    }

    public static async Task TimbreRuntimePage(ITimbreOutput output)
    {
        using var runtime = new TimbreRuntime(new TimbreRuntimeOptions { Output = output });
        using TimbreScope sounds = runtime.CreateScope();

        var confirm = new TimbreSound("audio/confirm.wav");
        await runtime.PrepareAsync(confirm);
        TimbrePlayback playback = sounds.Play(confirm);
        TimbrePlaybackResult result = await playback.Completion;
        _ = result;
    }

    public static TimbreRuntime TimbreRuntimeOptionsPage(ITimbreOutput output) =>
        new(new TimbreRuntimeOptions
        {
            Output = output,
            MaxVoices = 32,
            MaxCacheBytes = 32L * 1024 * 1024
        });

    public static void TimbreScopePage(TimbreScope sounds)
    {
        var plainTimbre = new TimbreSound("audio/confirm.wav");
        TimbrePlayback first = sounds.Play(plainTimbre);
        TimbrePlayback second = sounds.Play(plainTimbre); // overlaps: independent instances
        first.Cancel();                                 // second keeps playing

        TimbreHandle slot = sounds.CreateHandle();
        sounds.Play(plainTimbre, handle: slot);
        sounds.Play(plainTimbre, start => start.Volume = 0.5f, handle: slot); // replaces only the slot's occupant
        _ = second;
    }

    public static void TimbreStartOptionsPage(TimbreScope sounds, TimbreSound filteredTimbre, TimbreParameter<float> toneCutoff, TimbreHandle slot)
    {
        TimbrePlayback playback = sounds.Play(filteredTimbre, start =>
        {
            start.Volume = 0.2f;
            start.Loop = true;
            start.Set(toneCutoff, 800f);
        }, handle: slot);
        _ = playback;
    }

    public static void TimbreHandlePage(TimbreScope sounds, TimbreSound clip)
    {
        TimbreHandle slot = sounds.CreateHandle();
        TimbrePlayback old = sounds.Play(clip, handle: slot);
        TimbrePlayback current = sounds.Play(clip, handle: slot); // cancels old

        old.Cancel();  // no effect on current
        slot.Cancel(); // cancels current
        slot.Cancel(); // empty slot: no-op
        _ = current;
    }

    public static async Task TimbrePlaybackPage(TimbreScope sounds, TimbreSound filteredTimbre, TimbreParameter<float> toneCutoff)
    {
        TimbrePlayback playback = sounds.Play(filteredTimbre);
        playback.Volume = 0.8f;
        playback.Set(toneCutoff, 6000f);
        playback.Pause();
        playback.Resume();
        await playback.SeekAsync(TimeSpan.FromSeconds(1));

        TimbrePlaybackResult result = await playback.Completion;
        if (result.State == TimbrePlaybackState.Failed)
        {
            Console.WriteLine(result.Error!.Kind);
        }
    }

    public static async Task TimbrePlaybackResultPage(TimbrePlayback playback)
    {
        TimbrePlaybackResult result = await playback.Completion;
        switch (result.State)
        {
            case TimbrePlaybackState.Completed when result.TailTruncated:
                break; // the delay tail reached DelayTailCap
            case TimbrePlaybackState.Failed:
                Console.WriteLine($"{result.Error!.Kind}: {result.Error.Message}");
                break;
        }
    }

    public static async Task TimbreExceptionPage(TimbreRuntime runtime, TimbreSound clip)
    {
        try
        {
            await runtime.PrepareAsync(clip);
        }
        catch (TimbreException failure) when (failure.Kind == TimbreErrorKind.ResourceLimitExceeded)
        {
            // The decoded payload does not fit the configured preload or cache limit.
        }
    }

    public static void ApplicationPage(Button button)
    {
        // Element audio: scoped to the element's attachment lifecycle.
        TimbrePlayback click = button.Timbre.Play(new TimbreSound("audio/click.wav"));

        // Application audio: lives until the application exits.
        TimbrePlayback music = Application.Current!.Timbre.Play(
            new TimbreSound("audio/music.ogg", loading: TimbreLoading.Streaming),
            start => start.Loop = true);
        _ = (click, music);
    }

    public static TimbrePlayback UIElementPage(UIElement button, TimbreSound confirmTimbre) =>
        button.Timbre.Play(confirmTimbre);

    public static void TimbreDiagnosticsSnapshotPage(UIRoot root)
    {
        TimbreDiagnosticsSnapshot? sound = root.Detective.CaptureTimbre();
        if (sound is { OutputOpen: false, ActivePlaybacks: > 0 })
        {
            // Playbacks are waiting for an output that is not open.
        }
    }

    private sealed class MyToneReader : TimbreReader
    {
        public const int TableBytes = 64 * 1024;

        public override long? LengthFrames => 48000;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new TimbreReadResult(0, endOfSource: true));

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    // Verbatim from the TimbreReader page.
    private sealed class SilenceReader(long lengthFrames) : TimbreReader
    {
        private long position;

        public override long? LengthFrames => lengthFrames;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / TimbreRuntime.ChannelCount, lengthFrames - position);
            destination.Span[..(frames * TimbreRuntime.ChannelCount)].Clear();
            position += frames;
            return ValueTask.FromResult(new TimbreReadResult(frames, position == lengthFrames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, lengthFrames);
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
