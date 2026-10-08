using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using static Cerneala.Tests.Timbre.Motion.TimbreMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Identity, replacement and hidden-control lifecycle of audio Motion on the
// real Timbre engine, UIRoot Motion graph and element/markup scopes.
public sealed class TimbreMotionLifecycleTests
{
    private const string Clips = "<TimbreClip Name=\"Tone\">Source = \"audio/tone.wav\";</TimbreClip>";

    private static TweenSpec<float> Linear300 => new(TimeSpan.FromMilliseconds(300), Easings.Linear);

    [Fact]
    public void TimbreMotionCapturesThePlaybackInstanceNotTheSlot()
    {
        using MarkupTimbreFixture fixture = new("<Button Content=\"x\" />", "TimbreMotionIdentity.crn");
        Button button = (Button)fixture.Element;
        TimbreClip clip = new(TimbreSource.FromFile(MarkupTimbreFixture.ToneSource), volume: 0.2f);
        TimbreHandle slot = button.Timbre.CreateHandle();
        TimbrePlayback first = button.Timbre.Play(clip, handle: slot);
        Wait(() => fixture.Rig.StartAsync(first));

        MotionHandle animation = first.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear300);
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        Assert.InRange(first.Volume, 0.499f, 0.501f);

        TimbrePlayback second = button.Timbre.Play(clip, handle: slot);
        fixture.Pump(TimeSpan.FromMilliseconds(100));
        fixture.Pump(TimeSpan.FromMilliseconds(100));

        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
        Assert.True(animation.IsCanceled);
        Assert.InRange(first.Volume, 0.499f, 0.501f);
        Assert.Equal(0.2f, second.Volume);
    }

    [Fact]
    public void TimbreMotionReplacementMidAnimationStartsTheNewOccupantFromItsOwnValues()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><Button Content=\"Play\"><Button.Resources>" + Clips + "</Button.Resources><Button.Aspect>" +
            "@handle Playback; @on Click { @timbre $Tone(Volume = 0.2) as Playback; " +
            "@animate with Tween(300ms, Linear) { @to { $self.timbre.Playback.Volume = 0.8; } } }" +
            "</Button.Aspect></Button></StackPanel>",
            "TimbreMotionReplace.crn");

        Click(fixture);
        TimbrePlayback first = fixture.Started[0];
        Wait(() => fixture.Rig.StartAsync(first));
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        Assert.InRange(first.Volume, 0.499f, 0.501f);

        Click(fixture);
        TimbrePlayback second = fixture.Started[1];
        TimbreMotionTestKit.WaitPlaying(fixture, second);
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));

        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
        Assert.InRange(first.Volume, 0.499f, 0.501f);
        Assert.InRange(second.Volume, 0.499f, 0.501f);
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void TimbreMotionContinuesUnderAHiddenAncestorWhileVisualMotionIsCanceled(Visibility visibility)
    {
        using MarkupTimbreFixture fixture = new("<StackPanel><Button Content=\"x\" /></StackPanel>", "TimbreMotionAncestor.crn");
        StackPanel panel = (StackPanel)fixture.Element;
        Button button = fixture.All<Button>().Single();
        TimbrePlayback playback = button.Timbre.Play(new TimbreClip(TimbreSource.FromFile(MarkupTimbreFixture.ToneSource), volume: 0.2f));
        Wait(() => fixture.Rig.StartAsync(playback));
        MotionHandle audio = playback.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear300);
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

    private static void Click(MarkupTimbreFixture fixture) => fixture.ClickAsync("Play").GetAwaiter().GetResult();
}
