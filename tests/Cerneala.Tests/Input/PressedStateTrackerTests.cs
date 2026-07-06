using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class PressedStateTrackerTests
{
    [Fact]
    public void PressAndReleaseUpdateButtonPressedState()
    {
        ButtonBase button = new();
        PressedStateTracker tracker = new();

        tracker.Press(button);
        Assert.True(button.IsPressed);

        tracker.Release();
        Assert.False(button.IsPressed);
    }

    [Fact]
    public void PressingNoButtonCancelsExistingPressedState()
    {
        ButtonBase button = new();
        PressedStateTracker tracker = new();

        tracker.Press(button);
        tracker.Press(null);

        Assert.False(button.IsPressed);
        Assert.Null(tracker.PressedElement);
    }

    [Fact]
    public void PressingChildSetsAncestorPressedState()
    {
        ButtonBase button = new();
        UIElement child = new();
        button.VisualChildren.Add(child);
        PressedStateTracker tracker = new();

        tracker.Press(child);

        Assert.True(button.IsPressed);
        Assert.Same(button, tracker.PressedElement);
    }

    [Fact]
    public void CancelClearsExistingPressedState()
    {
        ButtonBase button = new();
        PressedStateTracker tracker = new();

        tracker.Press(button);
        tracker.Cancel();

        Assert.False(button.IsPressed);
        Assert.Null(tracker.PressedElement);
    }
}
