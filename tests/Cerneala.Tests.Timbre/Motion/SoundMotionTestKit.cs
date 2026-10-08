using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Timbre.Motion;

internal static class SoundMotionTestKit
{
    // Tone: constant PCM with a Cut parameter feeding LowPass (20–20000 Hz).
    public const string Clips =
        "<SoundClip Name=\"Tone\">Source = \"audio/tone.wav\"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</SoundClip>" +
        "<SoundClip Name=\"Ramp\">Source = \"audio/ramp.wav\";</SoundClip>";

    // A clickable "Play" button whose Aspect is `aspect`, under a panel that
    // declares Clips.
    public static string Panel(string aspect, string buttonAttributes = "") =>
        "<StackPanel><StackPanel.Resources>" + Clips + "</StackPanel.Resources>" +
        "<Button Content=\"Play\" " + buttonAttributes + "><Button.Aspect>" + aspect + "</Button.Aspect></Button></StackPanel>";

    // Harness waits run on the thread pool: their continuations must not be
    // posted to xUnit's bounded test context, which these synchronous
    // (UI-thread-affine) tests block while waiting.
    public static void Wait(Func<Task> wait) => Task.Run(wait).GetAwaiter().GetResult();

    public static void Wait<T>(Func<Task<T>> wait) => Task.Run(wait).GetAwaiter().GetResult();

    public static TweenSpec<float> Linear(int milliseconds) => new(TimeSpan.FromMilliseconds(milliseconds), Easings.Linear);

    public static Button PlayButton(MarkupSoundFixture fixture) => fixture.All<Button>().Single();

    public static SoundClip Resource(MarkupSoundFixture fixture, string name) =>
        PlayButton(fixture).FindResource(new ResourceId<SoundClip>(name));

    public static SoundParameter<float> CutOf(SoundClip clip) => (SoundParameter<float>)clip.Parameters.Single();

    public static float Cut(SoundPlayback playback) => playback.GetMotionSlotValue(1);

    public static void Click(MarkupSoundFixture fixture) => fixture.ClickAsync("Play").GetAwaiter().GetResult();

    // Root frames of `milliseconds` each; the root clamps a frame to 100 ms.
    public static void Advance(MarkupSoundFixture fixture, int frames, int milliseconds = 75)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(milliseconds));
        }
    }

    // Drains the sink until the mixer has produced the playback's first PCM.
    public static void WaitPlaying(MarkupSoundFixture fixture, SoundPlayback playback)
    {
        Wait(() => fixture.Rig.ReadyAsync(playback));
        fixture.Rig.Output.Release();
        for (int attempt = 0; attempt < 100 && playback.State == SoundPlaybackState.Pending; attempt++)
        {
            fixture.Rig.Output.ConsumeAll();
            Wait(() => fixture.Rig.SyncAsync());
        }

        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    public static void AssertNear(float expected, float actual, float tolerance = 0.001f) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
