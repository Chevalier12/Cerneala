using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Platform;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class MotionCompositionReducedMotionTests
{
    [Fact]
    public void SamePropertyReplacementCancelsOldHandle()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();

        MotionHandle first = element.Motion().Opacity.To(0.2f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        MotionHandle second = element.Motion().Opacity.To(0.8f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        Assert.True(first.IsCanceled);
        Assert.True(second.IsActive);
    }

    [Fact]
    public void PresenceUsesInternalCompositionChannelsWithoutOverwritingUserOpacityOrScale()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        Rectangle child = new()
        {
            Geometry = new RectangleGeometry(new Cerneala.Drawing.DrawRect(0, 0, 20, 20)),
            Opacity = 0.5f,
            Scale = 2f,
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
        };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();

        parent.VisualChildren.Remove(child);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.Equal(0.5f, child.Opacity);
        Assert.Equal(2f, child.Scale);
        Assert.True(child.PresenceOpacity < 1);
        Assert.True(child.PresenceScale < 1);
    }

    [Fact]
    public void ReducedMotionCompletesTweenQuicklyWithoutBreakingFinalTarget()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock, reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        value.AnimateTo(10f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));

        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(1));
        root.Motion.Tick();

        Assert.Equal(10f, value.Current);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void ReducedMotionRecordsSkippedDiagnosticForReducedTween()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock, reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
        root.Motion.Diagnostics.IsEnabled = true;
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(10f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));

        Assert.Equal(1, root.Motion.Diagnostics.ReducedMotionSkipCount);
        Assert.Contains(
            root.Motion.Diagnostics.Trace.Events,
            e => e.Kind == MotionTraceEventKind.MotionSkippedReducedMotion);
    }

    [Fact]
    public void PlatformReducedMotionSourceAffectsFutureAnimations()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        root.SetPlatformServices(new PlatformServices(ReducedMotion: new TestReducedMotionSource(ReducedMotionMode.Reduce)));
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);

        value.AnimateTo(10f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));

        Assert.Equal(ReducedMotionMode.Reduce, root.Motion.ReducedMotion.Mode);
        Assert.Equal(10f, value.Current);
    }

    [Fact]
    public void PlatformReducedMotionSourceDoesNotMutateOtherRoots()
    {
        UIRoot reducedRoot = new(100, 100);
        reducedRoot.SetPlatformServices(new PlatformServices(ReducedMotion: new TestReducedMotionSource(ReducedMotionMode.Reduce)));

        UIRoot defaultRoot = new(100, 100);
        MotionValue<float> value = defaultRoot.Motion.Graph.CreateValue(0f);

        value.AnimateTo(10f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        Assert.Equal(ReducedMotionMode.NoPreference, defaultRoot.Motion.ReducedMotion.Mode);
        Assert.True(defaultRoot.Motion.HasActiveMotion);
    }

    [Fact]
    public void ReducedMotionDisablesLayoutMotionCorrectionButKeepsFinalLayout()
    {
        UIRoot root = new(100, 100, reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.DisableNonEssential));
        Canvas canvas = new();
        UIElement child = new()
        {
            LayoutMotionId = "panel",
            LayoutMotion = LayoutMotionOptions.Spring(MotionFactory.Tween<Transform>(TimeSpan.FromMilliseconds(100)))
        };
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(child);
        root.ProcessFrame();

        Canvas.SetLeft(child, 40);
        root.ProcessFrame();

        Assert.Equal(40, child.ArrangedBounds.X);
        Assert.Null(root.Motion.Layout.GetBinding(child));
    }

    private sealed class TestReducedMotionSource(ReducedMotionMode mode) : IReducedMotionSource
    {
        public ReducedMotionMode Mode { get; } = mode;
    }
}
