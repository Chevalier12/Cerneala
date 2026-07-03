using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class GestureRecognizerTests
{
    [Fact]
    public void TapRecognizedWithinThreshold()
    {
        GestureRecognizer recognizer = new();

        Assert.Empty(recognizer.Process(new GestureSample(0, 0, true)));
        IReadOnlyList<GestureEvent> events = recognizer.Process(new GestureSample(1, 1, false));

        GestureEvent gesture = Assert.Single(events);
        Assert.Equal(GestureKind.Tap, gesture.Kind);
    }

    [Fact]
    public void ReleaseBeyondThresholdDoesNotRecognizeTap()
    {
        GestureRecognizer recognizer = new(dragThreshold: 4);

        Assert.Empty(recognizer.Process(new GestureSample(0, 0, true)));
        IReadOnlyList<GestureEvent> events = recognizer.Process(new GestureSample(5, 0, false));

        Assert.Empty(events);
    }

    [Fact]
    public void DragRecognizedAfterThreshold()
    {
        GestureRecognizer recognizer = new(dragThreshold: 4);

        recognizer.Process(new GestureSample(0, 0, true));
        IReadOnlyList<GestureEvent> start = recognizer.Process(new GestureSample(5, 0, true));
        IReadOnlyList<GestureEvent> delta = recognizer.Process(new GestureSample(7, 1, true));

        Assert.Equal(GestureKind.DragStarted, Assert.Single(start).Kind);
        GestureEvent dragDelta = Assert.Single(delta);
        Assert.Equal(GestureKind.DragDelta, dragDelta.Kind);
        Assert.Equal(2, dragDelta.DeltaX);
        Assert.Equal(1, dragDelta.DeltaY);
    }
}
