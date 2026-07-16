using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
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
