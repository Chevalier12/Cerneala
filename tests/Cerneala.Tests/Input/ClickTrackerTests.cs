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
}
