using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;
using static Cerneala.Tests.Timbre.Motion.TimbreMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Audio Motion: animations of a captured TimbrePlayback sampled by the owning
// root's Motion clock. Synchronous: the UI root is thread-affine.
public sealed class TimbreMotionIntegrationTests
{
    private static readonly TimeSpan TweenDuration = TimeSpan.FromMilliseconds(300);

    [Fact]
    public void TimbreMotionIsSampledByTheRootClockWhileTheControlIsHidden()
    {
        using MarkupTimbreFixture fixture = new("<Button Content=\"x\" />", "TimbreMotionHost.crn");
        Button button = (Button)fixture.Element;
        button.Visibility = Visibility.Collapsed;
        fixture.Pump();
        TimbrePlayback playback = button.Timbre.Play(new TimbreSound(TimbreSource.FromFile(MarkupTimbreFixture.ToneSource), volume: 0.2f));
        Wait(() => fixture.Rig.StartAsync(playback));

        playback.Motion()
            .Animate(TimbrePlayback.VolumeParameter)
            .To(0.8f)
            .With(new TweenSpec<float>(TweenDuration, Easings.Linear));
        fixture.Pump(); // first active root frame samples a zero delta
        fixture.Pump(TimeSpan.FromMilliseconds(75));
        fixture.Pump(TimeSpan.FromMilliseconds(75));

        Assert.InRange(playback.Volume, 0.499f, 0.501f);
    }
}
