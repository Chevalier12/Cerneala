using Border = Cerneala.UI.Controls.Border;
using ScrollContentPresenter = Cerneala.UI.Controls.ScrollContentPresenter;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Layout;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Layout;

public sealed class LayoutMotionCoordinatorTests
{
    [Fact]
    public void ScrollingDoesNotStartLayoutCorrection()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        StackPanel stack = new();
        Border card = new()
        {
            Height = 30,
            LayoutMotionId = "card",
            LayoutMotion = LayoutMotionOptions.Spring(
                MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)))
        };
        stack.VisualChildren.Add(new Border { Height = 60 });
        stack.VisualChildren.Add(card);
        stack.VisualChildren.Add(new Border { Height = 300 });
        ScrollContentPresenter presenter = new() { Width = 100, Height = 100, Content = stack };
        root.LogicalChildren.Add(presenter);
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.Equal(60, card.ArrangedBounds.Y);

        presenter.SetVerticalOffset(20);
        root.ProcessFrame();

        Assert.Equal(40, card.ArrangedBounds.Y);
        Assert.Equal(Cerneala.UI.Media.Transform.Identity, card.LayoutCorrectionTransform);
        Assert.Null(root.Motion.Layout.GetBinding(card));
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ScrollingOnBothAxesDoesNotCorrectTransformedOrNestedContent(bool transformed, bool nested)
    {
        (UIRoot root, ScrollContentPresenter presenter, Canvas content, UIElement card) =
            CreateScrollScenario(new ManualMotionClock());
        if (transformed)
        {
            presenter.Scale = 2;
            presenter.Rotation = 0.2f;
            content.Scale = 1.5f;
            card.Scale = 0.75f;
        }

        ScrollContentPresenter? outer = null;
        if (nested)
        {
            root.VisualChildren.Remove(presenter);
            Canvas outerContent = new() { Width = 500, Height = 500 };
            outerContent.VisualChildren.Add(presenter);
            outer = new ScrollContentPresenter { Width = 100, Height = 100, Content = outerContent };
            root.VisualChildren.Add(outer);
        }

        root.ProcessFrame();
        root.ProcessFrame();
        foreach (float offset in new[] { 20f, 40f, 10f, 20.25f, 40.6f, 10.9f, 0f })
        {
            presenter.SetHorizontalOffset(offset);
            presenter.SetVerticalOffset(offset);
            outer?.SetHorizontalOffset(offset / 2);
            outer?.SetVerticalOffset(offset / 2);
            root.ProcessFrame();

            Assert.Equal(Cerneala.UI.Media.Transform.Identity, card.LayoutCorrectionTransform);
            Assert.Null(root.Motion.Layout.GetBinding(card));
        }
    }

    [Fact]
    public void ScrollingAndLayoutMoveCorrectOnlyTheLayoutMove()
    {
        (UIRoot root, ScrollContentPresenter presenter, _, UIElement card) =
            CreateScrollScenario(new ManualMotionClock());
        root.ProcessFrame();
        root.ProcessFrame();

        presenter.SetVerticalOffset(20);
        Canvas.SetTop(card, 90);
        root.ProcessFrame();

        Assert.Equal(70, card.ArrangedBounds.Y);
        Assert.Equal(-30, card.LayoutCorrectionTransform.Matrix.M32);
    }

    [Fact]
    public void ScrollingDoesNotRestartAnActiveLayoutCorrection()
    {
        ManualMotionClock clock = new();
        (UIRoot root, ScrollContentPresenter presenter, _, UIElement card) = CreateScrollScenario(clock);
        root.ProcessFrame();
        root.ProcessFrame();
        Canvas.SetTop(card, 90);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        float correction = card.LayoutCorrectionTransform.Matrix.M32;
        Assert.InRange(correction, -29.99f, -0.01f);

        presenter.SetVerticalOffset(20);
        root.ProcessFrame();
        Assert.Equal(correction, card.LayoutCorrectionTransform.Matrix.M32);
        clock.Advance(TimeSpan.FromMilliseconds(60));
        root.ProcessFrame();

        Assert.Equal(Cerneala.UI.Media.Transform.Identity, card.LayoutCorrectionTransform);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void MovingTheScrollViewportStillStartsLayoutCorrection()
    {
        (UIRoot root, ScrollContentPresenter presenter, _, UIElement card) =
            CreateScrollScenario(new ManualMotionClock());
        root.VisualChildren.Remove(presenter);
        Canvas host = new();
        host.VisualChildren.Add(presenter);
        root.VisualChildren.Add(host);
        root.ProcessFrame();
        root.ProcessFrame();

        presenter.SetVerticalOffset(20);
        Canvas.SetTop(presenter, 30);
        root.ProcessFrame();

        Assert.Equal(-30, card.LayoutCorrectionTransform.Matrix.M32);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void FractionalScrollingWithMarginsDoesNotStartLayoutCorrection(float scale)
    {
        (UIRoot root, ScrollContentPresenter presenter, Canvas content, UIElement card) =
            CreateScrollScenario(new ManualMotionClock());
        root.SetViewport(100, 100, scale);
        content.Margin = new Thickness(0.25f);
        Canvas.SetTop(card, 60.25f);
        root.ProcessFrame();
        root.ProcessFrame();

        foreach (float offset in new[] { 0.25f, 0.6f, 0.9f, 20.25f, 0f })
        {
            presenter.SetVerticalOffset(offset);
            root.ProcessFrame();
            Assert.Equal(Cerneala.UI.Media.Transform.Identity, card.LayoutCorrectionTransform);
            Assert.Null(root.Motion.Layout.GetBinding(card));
        }
    }

    [Fact]
    public void WheelScrollingDoesNotStartLayoutCorrection()
    {
        (UIRoot root, ScrollContentPresenter presenter, Canvas content, UIElement card) =
            CreateScrollScenario(new ManualMotionClock());
        root.VisualChildren.Remove(presenter);
        presenter.Content = null;
        Cerneala.UI.Controls.ScrollViewer viewer = new() { Content = content };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        root.ProcessFrame();
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(10, 10);
        PointerSnapshot current = previous.WithWheelValue(-120);

        new ElementInputBridge().Dispatch(root,
            new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        root.ProcessFrame();

        Assert.Equal(48, viewer.Presenter.VerticalOffset);
        Assert.Equal(Cerneala.UI.Media.Transform.Identity, card.LayoutCorrectionTransform);
        Assert.Null(root.Motion.Layout.GetBinding(card));
    }

    [Fact]
    public void ScrolledContentItselfDoesNotStartLayoutCorrection()
    {
        (UIRoot root, ScrollContentPresenter presenter, Canvas content, UIElement card) =
            CreateScrollScenario(new ManualMotionClock());
        card.LayoutMotion = null;
        content.LayoutMotionId = "content";
        content.LayoutMotion = LayoutMotionOptions.Spring(
            MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        root.ProcessFrame();

        presenter.SetHorizontalOffset(20);
        presenter.SetVerticalOffset(30);
        root.ProcessFrame();

        Assert.Equal(Cerneala.UI.Media.Transform.Identity, content.LayoutCorrectionTransform);
        Assert.Null(root.Motion.Layout.GetBinding(content));
    }

    [Fact]
    public void ReparentingBetweenScrolledViewportsPreservesVisualContinuity()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Canvas host = new();
        Canvas firstContent = new() { Width = 500, Height = 500 };
        Canvas secondContent = new() { Width = 500, Height = 500 };
        ScrollContentPresenter first = new() { Width = 100, Height = 100, Content = firstContent };
        ScrollContentPresenter second = new() { Width = 100, Height = 100, Content = secondContent };
        Canvas.SetTop(second, 50);
        UIElement card = CreateLayoutElement("card");
        Canvas.SetTop(card, 60);
        firstContent.VisualChildren.Add(card);
        host.VisualChildren.Add(first);
        host.VisualChildren.Add(second);
        root.VisualChildren.Add(host);
        root.ProcessFrame();
        first.SetVerticalOffset(20);
        second.SetVerticalOffset(40);
        root.ProcessFrame();
        Assert.Equal(40, card.ArrangedBounds.Y);

        firstContent.VisualChildren.Remove(card);
        secondContent.VisualChildren.Add(card);
        root.ProcessFrame();

        Assert.Equal(70, card.ArrangedBounds.Y);
        Assert.Equal(-30, card.LayoutCorrectionTransform.Matrix.M32);
    }

    private static (UIRoot Root, ScrollContentPresenter Presenter, Canvas Content, UIElement Card)
        CreateScrollScenario(ManualMotionClock clock)
    {
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas content = new() { Width = 500, Height = 500 };
        FixedElement card = CreateLayoutElement("card");
        Canvas.SetTop(card, 60);
        content.VisualChildren.Add(card);
        ScrollContentPresenter presenter = new() { Width = 100, Height = 100, Content = content };
        root.VisualChildren.Add(presenter);
        return (root, presenter, content, card);
    }

    [Fact]
    public void SnapshotCaptureWithoutParticipantsDoesNotAllocateWithTreeSize()
    {
        UIRoot root = new(100, 100);
        Canvas canvas = new();
        root.VisualChildren.Add(canvas);
        for (int i = 0; i < 500; i++)
        {
            canvas.VisualChildren.Add(new UIElement());
        }

        root.ProcessFrame();
        canvas.Width = 80;
        Assert.True(root.LayoutQueue.HasWork);
        AssertEmptyCaptureAllocation(root);
    }

    [Fact]
    public void EffectiveParticipationChangesAfterAttachAreObserved()
    {
        ManualMotionClock clock = new();
        (UIRoot root, UIElement child) = CreateCanvasScenario(clock);
        child.ClearValue(UIElement.LayoutMotionOptionsProperty);
        child.ClearValue(UIElement.LayoutMotionIdProperty);
        root.ProcessFrame();

        child.SetValue(UIElement.LayoutMotionOptionsProperty,
            LayoutMotionOptions.Spring(MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100))),
            Cerneala.UI.Core.UiPropertyValueSource.AspectBase);
        child.SetValue<LayoutMotionId?>(UIElement.LayoutMotionIdProperty, "dynamic",
            Cerneala.UI.Core.UiPropertyValueSource.AspectBase);
        Canvas.SetLeft(child, 40);
        root.ProcessFrame();
        Assert.Equal(-40, Assert.IsType<LayoutMotionBinding>(root.Motion.Layout.GetBinding(child)).CurrentCorrection.Matrix.M31);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();
        child.LayoutMotion = null;
        Canvas.SetLeft(child, 60);
        root.ProcessFrame();
        Assert.Equal(Cerneala.UI.Media.Transform.Identity, child.LayoutCorrectionTransform);

        child.ClearValue(UIElement.LayoutMotionOptionsProperty);
        Canvas.SetLeft(child, 80);
        root.ProcessFrame();
        Assert.Equal(-20, Assert.IsType<LayoutMotionBinding>(root.Motion.Layout.GetBinding(child)).CurrentCorrection.Matrix.M31);
    }

    [Fact]
    public void RemovingLastParticipantRestoresEmptyCaptureCost()
    {
        (UIRoot root, UIElement child) = CreateCanvasScenario(new ManualMotionClock());
        UIElement canvas = root.VisualChildren[0];
        for (int i = 0; i < 500; i++)
        {
            canvas.VisualChildren.Add(new UIElement());
        }

        root.ProcessFrame();
        canvas.VisualChildren.Remove(child);
        root.ProcessFrame();
        canvas.Width = 80;
        AssertEmptyCaptureAllocation(root);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClearingEitherParticipationPropertyRestoresEmptyCaptureCost(bool clearId)
    {
        (UIRoot root, UIElement child) = CreateCanvasScenario(new ManualMotionClock());
        UIElement canvas = root.VisualChildren[0];
        for (int i = 0; i < 500; i++) canvas.VisualChildren.Add(new UIElement());
        root.ProcessFrame();

        if (clearId) child.ClearValue(UIElement.LayoutMotionIdProperty);
        else child.ClearValue(UIElement.LayoutMotionOptionsProperty);
        canvas.Width = 80;
        AssertEmptyCaptureAllocation(root);
    }

    [Fact]
    public void DetachedPropertyChangesAndCrossRootAttachDoNotKeepOldRootParticipating()
    {
        UIRoot first = new(100, 100);
        UIRoot second = new(100, 100);
        Canvas firstCanvas = new();
        Canvas secondCanvas = new();
        first.VisualChildren.Add(firstCanvas);
        second.VisualChildren.Add(secondCanvas);
        for (int i = 0; i < 500; i++) firstCanvas.VisualChildren.Add(new UIElement());
        DetachMutationElement child = new() { Width = 20, Height = 10 };
        firstCanvas.VisualChildren.Add(child);
        first.ProcessFrame();

        firstCanvas.VisualChildren.Remove(child);
        secondCanvas.VisualChildren.Add(child);
        second.ProcessFrame();
        Canvas.SetLeft(child, 40);
        second.ProcessFrame();

        Assert.Equal(-40, Assert.IsType<LayoutMotionBinding>(second.Motion.Layout.GetBinding(child)).CurrentCorrection.Matrix.M31);
        firstCanvas.Width = 80;
        AssertEmptyCaptureAllocation(first);
    }

    private sealed class DetachMutationElement : UIElement
    {
        protected override void OnDetached()
        {
            base.OnDetached();
            LayoutMotionId = "assigned-during-detach";
            LayoutMotion = LayoutMotionOptions.Spring(
                MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)));
        }
    }

    private static void AssertEmptyCaptureAllocation(UIRoot root)
    {
        for (int i = 0; i < 32; i++) root.Motion.Layout.CaptureFirstSnapshots();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 16; i++) root.Motion.Layout.CaptureFirstSnapshots();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated <= 4096, $"Empty layout-motion capture allocated {allocated} bytes for 16 captures.");
    }

    [Fact]
    public void ChangingArrangedRectCreatesRenderOnlyInverseCorrection()
    {
        ManualMotionClock clock = new();
        (UIRoot root, UIElement child) = CreateCanvasScenario(clock);
        root.ProcessFrame();

        Canvas.SetLeft(child, 40);
        FrameStats stats = root.ProcessFrame();

        LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(child);
        Assert.NotNull(binding);
        Assert.Equal(new LayoutRect(40, 0, 20, 10), child.ArrangedBounds);
        Assert.Equal(-40, binding.CurrentCorrection.Matrix.M31);
        Assert.Equal(0, binding.CurrentCorrection.Matrix.M32);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.True(stats.ArrangedElements > 0);
    }

    [Fact]
    public void LayoutMotionTickDoesNotEnqueueMeasureOrArrange()
    {
        ManualMotionClock clock = new();
        (UIRoot root, UIElement child) = CreateCanvasScenario(clock);
        root.ProcessFrame();
        Canvas.SetLeft(child, 40);
        root.ProcessFrame();

        clock.Advance(TimeSpan.FromMilliseconds(16));
        FrameStats stats = root.ProcessFrame();

        LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(child);
        Assert.NotNull(binding);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(1, stats.MotionFrames);
        Assert.True(binding.CurrentCorrection.Matrix.M31 > -40);
        Assert.True(binding.CurrentCorrection.Matrix.M31 < 0);
    }

    [Fact]
    public void MidFlightLayoutRetargetKeepsVisualContinuity()
    {
        ManualMotionClock clock = new();
        (UIRoot root, UIElement child) = CreateCanvasScenario(clock);
        root.ProcessFrame();
        Canvas.SetLeft(child, 40);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(child);
        Assert.NotNull(binding);
        float visualXBeforeRetarget = child.ArrangedBounds.X + binding.CurrentCorrection.Matrix.M31;

        Canvas.SetLeft(child, 80);
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(80, 0, 20, 10), child.ArrangedBounds);
        Assert.Equal(visualXBeforeRetarget, child.ArrangedBounds.X + binding.CurrentCorrection.Matrix.M31, precision: 3);
    }

    [Fact]
    public void LayoutMotionCompletesByClearingCorrection()
    {
        ManualMotionClock clock = new();
        (UIRoot root, UIElement child) = CreateCanvasScenario(clock);
        root.ProcessFrame();
        Canvas.SetLeft(child, 40);
        root.ProcessFrame();

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(child);
        Assert.NotNull(binding);
        Assert.Equal(Cerneala.UI.Media.Transform.Identity, binding.CurrentCorrection);
        Assert.False(root.Motion.HasActiveMotion);
        Assert.Equal(new LayoutRect(40, 0, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void CrossParentLayoutMotionConvertsAncestorRenderSpace()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas host = new();
        Canvas first = new();
        Canvas second = new();
        first.TranslateX = 30;
        Canvas.SetLeft(second, 40);
        FixedElement child = new(new LayoutSize(20, 10))
        {
            LayoutMotionId = "moving",
            LayoutMotion = LayoutMotionOptions.Spring(MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)))
        };
        root.VisualChildren.Add(host);
        host.VisualChildren.Add(first);
        host.VisualChildren.Add(second);
        first.VisualChildren.Add(child);
        root.ProcessFrame();

        first.VisualChildren.Remove(child);
        second.VisualChildren.Add(child);
        root.ProcessFrame();

        LayoutMotionBinding? binding = root.Motion.Layout.GetBinding(child);
        Assert.NotNull(binding);
        Assert.Equal(new LayoutRect(40, 0, 20, 10), child.ArrangedBounds);
        Assert.Equal(-10, binding.CurrentCorrection.Matrix.M31);
        Assert.Equal(0, binding.CurrentCorrection.Matrix.M32);
    }

    [Fact]
    public void DetachDisposesActiveLayoutCorrection()
    {
        ManualMotionClock clock = new();
        (UIRoot root, UIElement child) = CreateCanvasScenario(clock);
        root.ProcessFrame();
        Canvas.SetLeft(child, 40);
        root.ProcessFrame();

        Assert.Equal(1, root.Motion.Layout.ActiveBindingCount);
        Assert.True(root.VisualChildren[0].VisualChildren.Remove(child));
        root.ProcessFrame();

        Assert.Equal(0, root.Motion.Layout.ActiveBindingCount);
        Assert.Equal(Cerneala.UI.Media.Transform.Identity, child.LayoutCorrectionTransform);
    }

    [Fact]
    public void SameIdOnDifferentElementsDoesNotCreateSharedElementTransition()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas canvas = new();
        FixedElement first = CreateLayoutElement("shared");
        FixedElement replacement = CreateLayoutElement("shared");
        Canvas.SetLeft(replacement, 60);
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(first);
        root.ProcessFrame();

        canvas.VisualChildren.Remove(first);
        canvas.VisualChildren.Add(replacement);
        root.ProcessFrame();

        Assert.Null(root.Motion.Layout.GetBinding(replacement));
        Assert.Equal(Cerneala.UI.Media.Transform.Identity, replacement.LayoutCorrectionTransform);
    }

    private static (UIRoot Root, UIElement Child) CreateCanvasScenario(ManualMotionClock clock)
    {
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas canvas = new();
        FixedElement child = CreateLayoutElement("card");
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(child);
        return (root, child);
    }

    private static FixedElement CreateLayoutElement(string id)
    {
        return new FixedElement(new LayoutSize(20, 10))
        {
            LayoutMotionId = id,
            LayoutMotion = LayoutMotionOptions.Spring(MotionFactory.Tween<Cerneala.UI.Media.Transform>(TimeSpan.FromMilliseconds(100)))
        };
    }

    private sealed class FixedElement(LayoutSize desiredSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return desiredSize;
        }
    }
}
