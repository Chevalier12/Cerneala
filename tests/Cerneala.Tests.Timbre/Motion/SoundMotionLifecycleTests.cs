using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using static Cerneala.Tests.Timbre.Motion.SoundMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Identity, replacement and hidden-control lifecycle of audio Motion on the
// real Timbre engine, UIRoot Motion graph and element/markup scopes.
public sealed class SoundMotionLifecycleTests
{
    private const string Clips = "<SoundClip Name=\"Tone\">Source = \"audio/tone.wav\";</SoundClip>";

    private static TweenSpec<float> Linear300 => new(TimeSpan.FromMilliseconds(300), Easings.Linear);

    [Fact]
    public void SoundMotionCapturesThePlaybackInstanceNotTheSlot()
    {
        using MarkupSoundFixture fixture = new("<Button Content=\"x\" />", "SoundMotionIdentity.crn");
        Button button = (Button)fixture.Element;
        SoundClip clip = new(SoundSource.FromFile(MarkupSoundFixture.ToneSource), volume: 0.2f);
        SoundHandle slot = button.Sounds.CreateHandle();
        SoundPlayback first = button.Sounds.Play(clip, handle: slot);
        Wait(() => fixture.Rig.StartAsync(first));

        MotionHandle animation = first.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear300);
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        Assert.InRange(first.Volume, 0.499f, 0.501f);

        SoundPlayback second = button.Sounds.Play(clip, handle: slot);
        fixture.Pump(TimeSpan.FromMilliseconds(100));
        fixture.Pump(TimeSpan.FromMilliseconds(100));

        Assert.Equal(SoundPlaybackState.Canceled, first.State);
        Assert.True(animation.IsCanceled);
        Assert.InRange(first.Volume, 0.499f, 0.501f);
        Assert.Equal(0.2f, second.Volume);
    }

    [Fact]
    public void SoundMotionReplacementMidAnimationStartsTheNewOccupantFromItsOwnValues()
    {
        using MarkupSoundFixture fixture = new(
            "<StackPanel><Button Content=\"Play\"><Button.Resources>" + Clips + "</Button.Resources><Button.Aspect>" +
            "@handle Playback; @on Click { @sound $Tone(Volume = 0.2) as Playback; " +
            "@animate with Tween(300ms, Linear) { @to { $self.sound.Playback.Volume = 0.8; } } }" +
            "</Button.Aspect></Button></StackPanel>",
            "SoundMotionReplace.crn");

        Click(fixture);
        SoundPlayback first = fixture.Started[0];
        Wait(() => fixture.Rig.StartAsync(first));
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        Assert.InRange(first.Volume, 0.499f, 0.501f);

        Click(fixture);
        SoundPlayback second = fixture.Started[1];
        SoundMotionTestKit.WaitPlaying(fixture, second);
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));

        Assert.Equal(SoundPlaybackState.Canceled, first.State);
        Assert.InRange(first.Volume, 0.499f, 0.501f);
        Assert.InRange(second.Volume, 0.499f, 0.501f);
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void SoundMotionContinuesUnderAHiddenAncestorWhileVisualMotionIsCanceled(Visibility visibility)
    {
        using MarkupSoundFixture fixture = new("<StackPanel><Button Content=\"x\" /></StackPanel>", "SoundMotionAncestor.crn");
        StackPanel panel = (StackPanel)fixture.Element;
        Button button = fixture.All<Button>().Single();
        SoundPlayback playback = button.Sounds.Play(new SoundClip(SoundSource.FromFile(MarkupSoundFixture.ToneSource), volume: 0.2f));
        Wait(() => fixture.Rig.StartAsync(playback));
        MotionHandle audio = playback.Motion().Animate(SoundPlayback.VolumeParameter).To(0.8f).With(Linear300);
        MotionHandle visual = button.Motion().Opacity.To(0.2f, Linear300);

        panel.Visibility = visibility;
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));

        Assert.True(visual.IsCanceled);
        Assert.True(audio.IsActive);
        Assert.InRange(playback.Volume, 0.499f, 0.501f);

        panel.Visibility = Visibility.Visible;
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(10));

        Assert.True(audio.IsCompleted);
        Assert.Equal(0.8f, playback.Volume);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().PlaybacksStarted);
    }

    private static void Click(MarkupSoundFixture fixture) => fixture.ClickAsync("Play").GetAwaiter().GetResult();
}
