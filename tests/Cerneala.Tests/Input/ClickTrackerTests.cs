using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class ClickTrackerTests
{
    [Fact]
    public void MatchingReleaseReportsClick()
    {
        UIElement target = new();
        ClickTracker tracker = new();

        tracker.Press(target);
        int clickCount = tracker.Release(target);

        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void DifferentReleaseCancelsClick()
    {
        ClickTracker tracker = new();

        tracker.Press(new UIElement());
        int clickCount = tracker.Release(new UIElement());

        Assert.Equal(0, clickCount);
    }

    [Fact]
    public void CancelPreventsClickOnMatchingRelease()
    {
        UIElement target = new();
        ClickTracker tracker = new();

        tracker.Press(target);
        tracker.Cancel();
        int clickCount = tracker.Release(target);

        Assert.Equal(0, clickCount);
    }

    [Fact]
    public void StartPressCountsOneEvenInsideTheDoubleClickTimeAndSize()
    {
        UIElement target = new();
        ClickTracker tracker = new();

        tracker.Press(InputMouseButton.Left, target, 10, 10, TimeSpan.Zero);
        tracker.Release(InputMouseButton.Left, target);
        int count = tracker.Press(InputMouseButton.Left, target, 10, 10, TimeSpan.Zero, PressClickSequence.Start);

        Assert.Equal(1, count);
    }

    [Fact]
    public void ContinuePressIgnoresTheTimeButKeepsTheButtonTargetAndPositionTests()
    {
        UIElement target = new();
        ClickTracker tracker = new();

        tracker.Press(InputMouseButton.Left, target, 10, 10, TimeSpan.Zero, PressClickSequence.Start);
        tracker.Release(InputMouseButton.Left, target);
        int later = tracker.Press(InputMouseButton.Left, target, 10, 10, TimeSpan.FromSeconds(30), PressClickSequence.Continue);
        tracker.Release(InputMouseButton.Left, target);
        int moved = tracker.Press(InputMouseButton.Left, target, 50, 10, TimeSpan.FromSeconds(30), PressClickSequence.Continue);
        tracker.Release(InputMouseButton.Left, target);
        int otherTarget = tracker.Press(InputMouseButton.Left, new UIElement(), 50, 10, TimeSpan.FromSeconds(30), PressClickSequence.Continue);

        Assert.Equal(2, later);
        Assert.Equal(1, moved);
        Assert.Equal(1, otherTarget);
    }
}
