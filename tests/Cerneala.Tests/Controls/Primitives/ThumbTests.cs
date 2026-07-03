using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class ThumbTests
{
    [Fact]
    public void ThumbDragCapturesReportsDeltaAndReleases()
    {
        UIRoot root = new(100, 100);
        Thumb thumb = new();
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        root.VisualChildren.Add(thumb);
        ElementInputBridge bridge = new();
        List<DragDeltaEventArgs> deltas = [];
        thumb.DragDelta += (_, args) => deltas.Add(args);

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 12, 9, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(12, 9, 12, 9, previousDown: true));

        Assert.False(bridge.PointerCaptureManager.HasCapture);
        Assert.False(thumb.IsDragging);
        DragDeltaEventArgs delta = Assert.Single(deltas);
        Assert.Equal(7, delta.HorizontalChange);
        Assert.Equal(4, delta.VerticalChange);
    }

    [Fact]
    public void HandledPreviewMouseDownDoesNotStartDrag()
    {
        UIRoot root = new(100, 100);
        Thumb thumb = new();
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        root.VisualChildren.Add(thumb);
        root.Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, (_, args) => args.Handled = true);
        ElementInputBridge bridge = new();
        bool dragStarted = false;
        thumb.DragStarted += (_, _) => dragStarted = true;

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));

        Assert.False(dragStarted);
        Assert.False(thumb.IsDragging);
        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(float x, float y, bool currentDown = false)
    {
        return PointerFrame(x, y, x, y, currentDown: currentDown);
    }
}
