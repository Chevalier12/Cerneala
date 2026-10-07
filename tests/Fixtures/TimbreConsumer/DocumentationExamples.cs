using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;

namespace TimbreConsumer;

// The code samples of the canonical Timbre API pages, compiled against public
// API only. Each method holds the snippet of one page; parameters stand for the
// variables a page's snippet assumes.
public static class DocumentationExamples
{
    public static void SoundClipPage()
    {
        var plainSound = new SoundClip("audio/confirm.wav");

        var toneCutoff = new SoundParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
        var filteredSound = new SoundClip(
            source: "audio/confirm.wav",
            parameters: [toneCutoff],
            modifiers: [new LowPass(cutoff: toneCutoff)]);

        var echo = new SoundClip(
            "audio/confirm.wav",
            modifiers: [new LowPass(cutoff: 1200f), new Delay(time: 0.12f, feedback: 0.2f, mix: 0.15f)]);
        _ = (plainSound, filteredSound, echo);
    }

    public static void SoundSourcePage()
    {
        SoundSource file = "audio/confirm.wav";
        SoundSource stream = SoundSource.FromStream(() => File.OpenRead("audio/music.ogg"), name: "music");
        SoundSource generated = SoundSource.FromReader(() => new MyToneReader(), name: "tone");
        _ = (file, stream, generated);
    }

    public static SoundClip SoundReaderPage() =>
        new(SoundSource.FromReader(() => new SilenceReader(48000)));

    public static void SoundReadResultPage()
    {
        var last = new SoundReadResult(frames: 120, endOfSource: true);
        var waiting = new SoundReadResult(frames: 0, endOfSource: false);
        _ = (last, waiting);
    }

    public static SoundClip SoundLoadingPage() =>
        new("audio/music.ogg", loop: true, loading: SoundLoading.Streaming);

    public static void SoundParameterPage(SoundScope sounds)
    {
        var toneCutoff = new SoundParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
        var clip = new SoundClip("audio/confirm.wav", parameters: [toneCutoff], modifiers: [new LowPass(cutoff: toneCutoff)]);

        SoundPlayback playback = sounds.Play(clip, start => start.Set(toneCutoff, 800f));
        playback.Set(toneCutoff, 6000f);
    }

    public static void SoundInputPage()
    {
        var toneCutoff = new SoundParameter<float>("ToneCutoff", 1200f);
        var fixedFilter = new LowPass(cutoff: 1200f);       // constant
        var driven = new LowPass(cutoff: toneCutoff);       // parameter
        _ = (fixedFilter, driven);
    }

    public static void LowPassPage()
    {
        var cutoff = new SoundParameter<float>("ToneCutoff", 1200f);
        var clip = new SoundClip("audio/confirm.wav", parameters: [cutoff], modifiers: [new LowPass(cutoff: cutoff)]);
        var fixedFilter = new LowPass(cutoff: 800f);
        _ = (clip, fixedFilter);
    }

    public static SoundClip DelayPage()
    {
        var echoMix = new SoundParameter<float>("EchoMix", 0.15f);
        return new SoundClip(
            "audio/confirm.wav",
            parameters: [echoMix],
            modifiers: [new Delay(time: 0.12f, feedback: 0.20f, mix: echoMix)]);
    }

    public static async Task SoundRuntimePage(ISoundOutput output)
    {
        using var runtime = new SoundRuntime(new SoundRuntimeOptions { Output = output });
        using SoundScope sounds = runtime.CreateScope();

        var confirm = new SoundClip("audio/confirm.wav");
        await runtime.PrepareAsync(confirm);
        SoundPlayback playback = sounds.Play(confirm);
        SoundPlaybackResult result = await playback.Completion;
        _ = result;
    }

    public static SoundRuntime SoundRuntimeOptionsPage(ISoundOutput output) =>
        new(new SoundRuntimeOptions
        {
            Output = output,
            MaxVoices = 32,
            MaxCacheBytes = 32L * 1024 * 1024
        });

    public static void SoundScopePage(SoundScope sounds)
    {
        var plainSound = new SoundClip("audio/confirm.wav");
        SoundPlayback first = sounds.Play(plainSound);
        SoundPlayback second = sounds.Play(plainSound); // overlaps: independent instances
        first.Cancel();                                 // second keeps playing

        SoundHandle slot = sounds.CreateHandle();
        sounds.Play(plainSound, handle: slot);
        sounds.Play(plainSound, start => start.Volume = 0.5f, handle: slot); // replaces only the slot's occupant
        _ = second;
    }

    public static void SoundStartOptionsPage(SoundScope sounds, SoundClip filteredSound, SoundParameter<float> toneCutoff, SoundHandle slot)
    {
        SoundPlayback playback = sounds.Play(filteredSound, start =>
        {
            start.Volume = 0.2f;
            start.Loop = true;
            start.Set(toneCutoff, 800f);
        }, handle: slot);
        _ = playback;
    }

    public static void SoundHandlePage(SoundScope sounds, SoundClip clip)
    {
        SoundHandle slot = sounds.CreateHandle();
        SoundPlayback old = sounds.Play(clip, handle: slot);
        SoundPlayback current = sounds.Play(clip, handle: slot); // cancels old

        old.Cancel();  // no effect on current
        slot.Cancel(); // cancels current
        slot.Cancel(); // empty slot: no-op
        _ = current;
    }

    public static async Task SoundPlaybackPage(SoundScope sounds, SoundClip filteredSound, SoundParameter<float> toneCutoff)
    {
        SoundPlayback playback = sounds.Play(filteredSound);
        playback.Volume = 0.8f;
        playback.Set(toneCutoff, 6000f);
        playback.Pause();
        playback.Resume();
        await playback.SeekAsync(TimeSpan.FromSeconds(1));

        SoundPlaybackResult result = await playback.Completion;
        if (result.State == SoundPlaybackState.Failed)
        {
            Console.WriteLine(result.Error!.Kind);
        }
    }

    public static async Task SoundPlaybackResultPage(SoundPlayback playback)
    {
        SoundPlaybackResult result = await playback.Completion;
        switch (result.State)
        {
            case SoundPlaybackState.Completed when result.TailTruncated:
                break; // the delay tail reached DelayTailCap
            case SoundPlaybackState.Failed:
                Console.WriteLine($"{result.Error!.Kind}: {result.Error.Message}");
                break;
        }
    }

    public static async Task SoundExceptionPage(SoundRuntime runtime, SoundClip clip)
    {
        try
        {
            await runtime.PrepareAsync(clip);
        }
        catch (SoundException failure) when (failure.Kind == SoundErrorKind.ResourceLimitExceeded)
        {
            // The decoded payload does not fit the configured preload or cache limit.
        }
    }

    public static void ApplicationPage(Button button)
    {
        // Element audio: scoped to the element's attachment lifecycle.
        SoundPlayback click = button.Sounds.Play(new SoundClip("audio/click.wav"));

        // Application audio: lives until the application exits.
        SoundPlayback music = Application.Current!.Sounds.Play(
            new SoundClip("audio/music.ogg", loading: SoundLoading.Streaming),
            start => start.Loop = true);
        _ = (click, music);
    }

    public static SoundPlayback UIElementPage(UIElement button, SoundClip confirmSound) =>
        button.Sounds.Play(confirmSound);

    public static void SoundDiagnosticsSnapshotPage(UIRoot root)
    {
        SoundDiagnosticsSnapshot? sound = root.Detective.CaptureSound();
        if (sound is { OutputOpen: false, ActivePlaybacks: > 0 })
        {
            // Playbacks are waiting for an output that is not open.
        }
    }

    private sealed class MyToneReader : SoundReader
    {
        public override long? LengthFrames => 48000;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new SoundReadResult(0, endOfSource: true));

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    // Verbatim from the SoundReader page.
    private sealed class SilenceReader(long lengthFrames) : SoundReader
    {
        private long position;

        public override long? LengthFrames => lengthFrames;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / SoundRuntime.ChannelCount, lengthFrames - position);
            destination.Span[..(frames * SoundRuntime.ChannelCount)].Clear();
            position += frames;
            return ValueTask.FromResult(new SoundReadResult(frames, position == lengthFrames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, lengthFrames);
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
