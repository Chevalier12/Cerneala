using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion;

public sealed class MotionStressTests
{
    [Fact]
    public void StressTest100SimultaneousRenderOnlyAnimations()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        List<UIElement> elements = [];
        for (int i = 0; i < 100; i++)
        {
            UIElement element = new();
            elements.Add(element);
            root.VisualChildren.Add(element);
        }

        root.ProcessFrame();
        foreach (UIElement element in elements)
        {
            element.Motion().Opacity.To(0.5f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(10)));
        }

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(10));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void StressTestRetargetingEveryFrameFor60Frames()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        for (int i = 0; i < 60; i++)
        {
            element.Motion().TranslateX.To(i, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(16)));
            clock.Advance(TimeSpan.FromMilliseconds(16));
            root.ProcessFrame();
        }

        Assert.True(float.IsFinite(element.TranslateX));
    }

    [Fact]
    public void StressTestReducedMotionTogglingFutureAnimations()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock, reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        element.Motion().Opacity.To(0.2f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));
        root.ProcessFrame();

        Assert.Equal(0.2f, element.Opacity);
    }

    [Fact]
    public void StressTestLayoutReorderWith100ElementsUsesRenderOnlyCorrectionTicks()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(1000, 1000, motionClock: clock);
        Canvas canvas = new();
        List<UIElement> elements = [];
        root.VisualChildren.Add(canvas);
        for (int i = 0; i < 100; i++)
        {
            FixedElement element = new(new LayoutSize(10, 10))
            {
                LayoutMotionId = $"item-{i}",
                LayoutMotion = LayoutMotionOptions.Spring(MotionFactory.Tween<Transform>(TimeSpan.FromMilliseconds(100)))
            };
            Canvas.SetLeft(element, i * 10);
            elements.Add(element);
            canvas.VisualChildren.Add(element);
        }

        root.ProcessFrame();
        for (int i = 0; i < elements.Count; i++)
        {
            Canvas.SetLeft(elements[i], (elements.Count - 1 - i) * 10);
        }

        FrameStats reorderStats = root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(16));
        FrameStats correctionTickStats = root.ProcessFrame();

        Assert.True(reorderStats.ArrangedElements > 0);
        Assert.Equal(0, correctionTickStats.MeasuredElements);
        Assert.Equal(0, correctionTickStats.ArrangedElements);
        Assert.All(elements, element => Assert.NotNull(root.Motion.Layout.GetBinding(element)));
    }

    [Fact]
    public void StressTestPresenceExitCancellationWith100ElementsKeepsElementsAttached()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(1000, 1000, motionClock: clock);
        Canvas parent = new();
        List<Rectangle> elements = [];
        root.VisualChildren.Add(parent);
        for (int i = 0; i < 100; i++)
        {
            Rectangle element = new()
            {
                Fill = new SolidColorBrush(DrawColor.White),
                Geometry = new RectangleGeometry(new DrawRect(0, 0, 10, 10)),
                Presence = PresenceOptions.FadeAndScale(
                    MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
                    MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
            };
            elements.Add(element);
            parent.VisualChildren.Add(element);
        }

        root.ProcessFrame();
        foreach (Rectangle element in elements)
        {
            Assert.True(parent.VisualChildren.Remove(element));
        }

        foreach (Rectangle element in elements)
        {
            parent.VisualChildren.Add(element);
        }

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(100, parent.VisualChildren.Count);
        Assert.Equal(0, root.Motion.Presence.ActiveExitCount);
        Assert.All(elements, element =>
        {
            Assert.True(element.IsAttached);
            Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(element));
        });
    }

    private sealed class FixedElement(LayoutSize desiredSize) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return desiredSize;
        }
    }
}
