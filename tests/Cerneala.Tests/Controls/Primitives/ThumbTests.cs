using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

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
    public void DragChangesUseTheSpaceWhereTheThumbIsArranged()
    {
        UIRoot root = new(100, 100);
        UIElement scaledParent = new()
        {
            RenderTransform = new Transform(Matrix3x2.CreateScale(2, 2)),
            RenderTransformOrigin = new LayoutPoint(0, 0)
        };
        Thumb thumb = new();
        scaledParent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        scaledParent.VisualChildren.Add(thumb);
        root.VisualChildren.Add(scaledParent);
        ElementInputBridge bridge = new();
        List<DragDeltaEventArgs> deltas = [];
        List<DragCompletedEventArgs> completed = [];
        thumb.DragDelta += (_, args) => deltas.Add(args);
        thumb.DragCompleted += (_, args) => completed.Add(args);

        // The parent draws everything 2x larger, so 14 x 8 root pixels are 7 x 4 layout pixels.
        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 24, 18, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(24, 18, 24, 18, previousDown: true));

        DragDeltaEventArgs delta = Assert.Single(deltas);
        Assert.Equal((7f, 4f), (delta.HorizontalChange, delta.VerticalChange));
        Assert.Equal((7f, 4f), (delta.TotalHorizontalChange, delta.TotalVerticalChange));
        Assert.Equal((7f, 4f), (thumb.TotalHorizontalChange, thumb.TotalVerticalChange));
        DragCompletedEventArgs done = Assert.Single(completed);
        Assert.Equal((7f, 4f), (done.HorizontalChange, done.VerticalChange));
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

    [Fact]
    public void DragCancelsWhenThumbIsDetachedBeforeRelease()
    {
        UIRoot root = new(100, 100);
        Thumb thumb = new();
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        root.VisualChildren.Add(thumb);
        ElementInputBridge bridge = new();
        List<DragCompletedEventArgs> completed = [];
        thumb.DragCompleted += (_, args) => completed.Add(args);

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        root.VisualChildren.Remove(thumb);
        bridge.Dispatch(root, PointerFrame(5, 5, 10, 10, previousDown: true, currentDown: true));

        Assert.False(bridge.PointerCaptureManager.HasCapture);
        Assert.False(thumb.IsDragging);
        DragCompletedEventArgs args = Assert.Single(completed);
        Assert.True(args.Canceled);
    }

    // A drag ends with one canceled DragCompleted when the thumb, or an ancestor, stops being able to take input while
    // the button is down, as it does when the thumb is detached (#952).
    [Theory]
    [InlineData("none")]
    [InlineData("detach")]
    [InlineData("hidden")]
    [InlineData("collapsed")]
    [InlineData("disabled")]
    [InlineData("parentCollapsed")]
    [InlineData("parentHidden")]
    public void DragEndsWhenTheThumbStopsBeingAbleToTakeInputBeforeRelease(string change)
    {
        UIRoot root = new(100, 100);
        UIElement parent = new();
        Thumb thumb = new();
        parent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        parent.VisualChildren.Add(thumb);
        root.VisualChildren.Add(parent);
        ElementInputBridge bridge = new();
        List<DragCompletedEventArgs> completed = [];
        thumb.DragCompleted += (_, args) => completed.Add(args);

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        Assert.True(thumb.IsDragging);
        switch (change)
        {
            case "detach": parent.VisualChildren.Remove(thumb); break;
            case "hidden": thumb.Visibility = Visibility.Hidden; break;
            case "collapsed": thumb.Visibility = Visibility.Collapsed; break;
            case "disabled": thumb.IsEnabled = false; break;
            case "parentCollapsed": parent.Visibility = Visibility.Collapsed; break;
            case "parentHidden": parent.Visibility = Visibility.Hidden; break;
        }

        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(5, 5, 10, 10, previousDown: true, currentDown: true));

        string state = $"row={change}: IsDragging={thumb.IsDragging}, DragCompleted count={completed.Count}, " +
            $"canceled=[{string.Join(",", completed.Select(args => args.Canceled))}], HasCapture={bridge.PointerCaptureManager.HasCapture}";
        if (change == "none")
        {
            Assert.True(thumb.IsDragging && completed.Count == 0, state);
        }
        else
        {
            Assert.True(!thumb.IsDragging && completed.Count == 1 && completed[0].Canceled, state);
        }
    }

    // After the button was released and the thumb is visible and enabled again, a plain move over it reaches the parent.
    [Theory]
    [InlineData("none")]
    [InlineData("hidden")]
    [InlineData("disabled")]
    public void ThumbDoesNotSwallowLaterPointerMovesAfterItBecameUsableAgain(string change)
    {
        UIRoot root = new(100, 100);
        UIElement parent = new();
        Thumb thumb = new();
        parent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        parent.VisualChildren.Add(thumb);
        root.VisualChildren.Add(parent);
        ElementInputBridge bridge = new();
        int parentMoves = 0;
        parent.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) => parentMoves++);

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        Assert.True(thumb.IsDragging);
        switch (change)
        {
            case "hidden": thumb.Visibility = Visibility.Hidden; break;
            case "disabled": thumb.IsEnabled = false; break;
        }

        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(5, 5, 10, 10, previousDown: true, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, 10, 10, previousDown: true));
        switch (change)
        {
            case "hidden": thumb.Visibility = Visibility.Visible; break;
            case "disabled": thumb.IsEnabled = true; break;
        }

        root.ProcessFrame();
        int before = parentMoves;
        bridge.Dispatch(root, PointerFrame(10, 10, 12, 12));

        Assert.True(
            parentMoves > before && !thumb.IsDragging,
            $"row={change}: IsDragging={thumb.IsDragging}, ancestor saw the plain move: {parentMoves > before} (moves before={before}, after={parentMoves}).");
    }

    // The drag of a hidden thumb ends when the dispatch resolves the pointer target, and a DragCompleted handler can change
    // the tree there: the pointer events of that frame go to the tree as the handler left it, not to an element it removed.
    [Fact]
    public void ElementRemovedByTheDragCompletedOfAHiddenThumbGetsNoPointerEvents()
    {
        UIRoot root = new(100, 100);
        UIElement parent = new();
        Thumb thumb = new();
        UIElement removed = new();
        parent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        removed.Arrange(new ArrangeContext(new LayoutRect(40, 40, 40, 40)));
        parent.VisualChildren.Add(thumb);
        parent.VisualChildren.Add(removed);
        root.VisualChildren.Add(parent);
        ElementInputBridge bridge = new();
        bool isRemoved = false;
        List<string> eventsAfterRemoval = [];
        removed.Handlers.AddHandler(InputEvents.MouseEnterEvent, (_, _) => { if (isRemoved) eventsAfterRemoval.Add("MouseEnter"); });
        removed.Handlers.AddHandler(InputEvents.MouseMoveEvent, (_, _) => { if (isRemoved) eventsAfterRemoval.Add("MouseMove"); });
        thumb.DragCompleted += (_, _) =>
        {
            parent.VisualChildren.Remove(removed);
            isRemoved = true;
        };

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));
        bridge.Dispatch(root, PointerFrame(5, 5, 50, 50, previousDown: true, currentDown: true));
        thumb.Visibility = Visibility.Hidden;
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(50, 50, 55, 55, previousDown: true, currentDown: true));

        Assert.True(
            isRemoved && eventsAfterRemoval.Count == 0 && !removed.IsMouseOver,
            $"removed={isRemoved}, events after removal=[{string.Join(",", eventsAfterRemoval)}], IsMouseOver={removed.IsMouseOver}");
    }

    [Fact]
    public void ThumbCaptureMonopolizesPointerInputUntilLeftButtonRelease()
    {
        UIRoot root = new(120, 40);
        Thumb thumb = new();
        UIElement other = new();
        thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
        other.Arrange(new ArrangeContext(new LayoutRect(60, 0, 20, 20)));
        root.VisualChildren.Add(thumb);
        root.VisualChildren.Add(other);
        ElementInputBridge bridge = new();
        int rootWheelCount = 0;
        int otherMouseDownCount = 0;
        root.Handlers.AddHandler(InputEvents.MouseWheelEvent, (_, _) => rootWheelCount++);
        other.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => otherMouseDownCount++);

        bridge.Dispatch(root, PointerFrame(5, 5, currentDown: true));

        PointerSnapshot previous = PointerSnapshot.Empty
            .WithPosition(5, 5)
            .WithButton(InputMouseButton.Left, true);
        PointerSnapshot current = PointerSnapshot.Empty
            .WithPosition(65, 5)
            .WithWheelValue(120)
            .WithButton(InputMouseButton.Left, true)
            .WithButton(InputMouseButton.Right, true);
        bridge.Dispatch(root, new InputFrame(
            previous,
            current,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []));

        Assert.Same(thumb, bridge.PointerCaptureManager.CapturedElement);
        Assert.False(other.IsPointerOver);
        Assert.Equal(0, rootWheelCount);
        Assert.Equal(0, otherMouseDownCount);

        bridge.Dispatch(root, PointerFrame(65, 5, 65, 5, previousDown: true));

        Assert.False(bridge.PointerCaptureManager.HasCapture);
        Assert.False(thumb.IsDragging);
    }

    [Fact]
    public void PositiveUniformBorderIsDrawnOutsideTheArrangedBoundsLikeABorder()
    {
        Thumb thumb = new() { BorderBrush = new SolidColorBrush(Color.Black), BorderThickness = new Thickness(2) };

        DrawCommandList commands = RenderAt(thumb, new LayoutRect(0, 0, 20, 20));

        // The centred pen of thickness 2 on the bounds inflated by 1 covers the 2px just outside (0, 0, 20, 20).
        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, thumb.BorderBrush));
        Assert.Equal(DrawCommandKind.DrawRectangle, border.Kind);
        Assert.Equal(new DrawRect(-1, -1, 22, 22), border.Rect);
        Assert.Equal(2, border.Thickness);
    }

    [Fact]
    public void NegativeUniformBorderIsDrawnInsideTheArrangedBoundsLikeABorder()
    {
        Thumb thumb = new() { BorderBrush = new SolidColorBrush(Color.Black), BorderThickness = new Thickness(-2) };

        DrawCommandList commands = RenderAt(thumb, new LayoutRect(0, 0, 20, 20));

        // The centred pen of thickness 2 on the bounds deflated by 1 covers the 2px just inside (0, 0, 20, 20).
        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, thumb.BorderBrush));
        Assert.Equal(DrawCommandKind.DrawRectangle, border.Kind);
        Assert.Equal(new DrawRect(1, 1, 18, 18), border.Rect);
        Assert.Equal(2, border.Thickness);
    }

    [Fact]
    public void SingleSideBorderIsOneEvenOddPathLikeABorder()
    {
        Thumb thumb = new() { BorderBrush = new SolidColorBrush(Color.Black), BorderThickness = new Thickness(0, 0, 0, 2) };

        DrawCommandList commands = RenderAt(thumb, new LayoutRect(0, 0, 20, 20));

        DrawCommand border = Assert.Single(commands, command => ReferenceEquals(command.Brush, thumb.BorderBrush));
        Assert.Equal(DrawCommandKind.FillPath, border.Kind);
        Assert.Equal(DrawFillRule.EvenOdd, border.FillRule);
    }

    private static DrawCommandList RenderAt(Thumb thumb, LayoutRect bounds)
    {
        UIRoot root = new();
        root.VisualChildren.Add(thumb);
        root.ProcessFrame();
        thumb.Arrange(new ArrangeContext(bounds));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
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
