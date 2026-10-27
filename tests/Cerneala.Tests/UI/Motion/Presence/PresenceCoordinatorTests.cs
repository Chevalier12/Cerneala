using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Motion.Specs;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Presence;

public sealed class PresenceCoordinatorTests
{
    [Fact]
    public void PublicApiDoesNotExposeAnUnobtainablePresenceHandle()
    {
        Assert.Null(typeof(PresenceCoordinator).Assembly.GetType(
            "Cerneala.UI.Motion.Presence.PresenceHandle"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExcludeInputWhileExitingDecidesWhetherAnExitingElementIsHitTested(bool excludeInputWhileExiting)
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreatePresentScenario(clock, excludeInputWhileExiting);
        Assert.Same(child, new HitTestService().HitTest(root, 5, 5)?.Element);

        parent.VisualChildren.Remove(child);
        root.ProcessFrame();
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        UIElement? hit = new HitTestService().HitTest(root, 5, 5)?.Element;

        Assert.True(
            ReferenceEquals(hit, child) == !excludeInputWhileExiting,
            $"excludeInputWhileExiting={excludeInputWhileExiting}: the exiting child {(ReferenceEquals(hit, child) ? "is" : "is not")} hit tested (hit: {hit?.GetType().Name ?? "nothing"}).");

        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.NotSame(child, new HitTestService().HitTest(root, 5, 5)?.Element);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExcludeInputWhileExitingDecidesWhetherAPressOnAnExitingElementRoutesThroughItToItsFormerParent(
        bool excludeInputWhileExiting)
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreatePresentScenario(clock, excludeInputWhileExiting);
        ElementInputBridge bridge = new();
        List<object> parentSources = [];
        int childPresses = 0;
        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => childPresses++);
        parent.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, args) => parentSources.Add(args.OriginalSource));

        parent.VisualChildren.Remove(child);
        root.ProcessFrame();
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        bridge.Dispatch(root, PressFrame(5, 5));

        Assert.Equal(excludeInputWhileExiting ? 0 : 1, childPresses);
        Assert.Equal(excludeInputWhileExiting ? parent : child, Assert.Single(parentSources));
    }

    [Fact]
    public void RepeatedAttachDetachDoesNotAccumulateEnterMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        Rectangle child = CreateChild();
        root.VisualChildren.Add(parent);

        for (int cycle = 0; cycle < 100; cycle++)
        {
            parent.VisualChildren.Add(child);
            Assert.Equal(2, root.Motion.Graph.ActiveNodeCount);

            child.Presence = null;
            Assert.True(parent.VisualChildren.Remove(child));
            Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
            child.Presence = CreatePresenceOptions();
        }
    }

    [Fact]
    public void ExitHandoffReplacesActiveEnterMotion()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);

        Assert.Equal(2, root.Motion.Graph.ActiveNodeCount);
        Assert.True(parent.VisualChildren.Remove(child));

        Assert.Equal(2, root.Motion.Graph.ActiveNodeCount);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
    }

    [Fact]
    public void EnterAppliesInitialVisualStateAndAnimatesToPresent()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        Rectangle child = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20)),
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
        };
        root.VisualChildren.Add(parent);

        parent.VisualChildren.Add(child);

        Assert.True(child.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Equal(0, child.PresenceOpacity);
        Assert.True(child.PresenceScale < 1);
        Assert.True(root.Motion.HasActiveMotion);

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(1, child.PresenceOpacity);
        Assert.Equal(1, child.PresenceScale);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void ExitKeepsElementAttachedAndRenderableUntilCompletion()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);
        root.ProcessFrame();

        Assert.True(parent.VisualChildren.Remove(child));

        Assert.DoesNotContain(child, parent.VisualChildren);
        Assert.Contains(child, root.Motion.Presence.GetExitingVisualChildren(parent));
        Assert.True(child.IsAttached);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.True(root.Motion.HasActiveMotion);
        root.RenderQueue.Enqueue(child);
        Assert.Contains(child, root.RenderQueue.Snapshot());
    }

    [Fact]
    public void ExitRemoveDecreasesPublicCollectionCount()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);
        root.ProcessFrame();

        Assert.True(parent.VisualChildren.Remove(child));

        Assert.Empty(parent.VisualChildren);
        Assert.True(child.IsAttached);
        Assert.Contains(child, root.Motion.Presence.GetExitingVisualChildren(parent));
    }

    [Fact]
    public void ExitingElementDoesNotReceiveInputByDefault()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);
        root.ProcessFrame();
        Assert.Same(child, new HitTestService().HitTest(root, 5, 5)?.Element);

        parent.VisualChildren.Remove(child);
        root.ProcessFrame();

        Assert.NotSame(child, new HitTestService().HitTest(root, 5, 5)?.Element);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InputParticipationWhileExitingFollowsOnlyTheInputOption(bool excludeInputWhileExiting)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        Rectangle child = CreateChild();
        child.Presence = PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            excludeInputWhileExiting);
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();

        parent.VisualChildren.Remove(child);

        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal(!excludeInputWhileExiting, UIElementVisibility.ParticipatesInInput(child));
        Assert.Equal(!excludeInputWhileExiting, UIElementVisibility.ParticipatesInHitTest(child));
    }

    [Fact]
    public void ExitCompletionRemovesElementOnce()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);
        root.ProcessFrame();
        parent.VisualChildren.Remove(child);
        root.ProcessFrame();

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.DoesNotContain(child, parent.VisualChildren);
        Assert.False(child.IsAttached);
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
    }

    [Fact]
    public void ReAddingWhileExitingCancelsExitAndRestoresPresentState()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);
        root.ProcessFrame();
        parent.VisualChildren.Remove(child);

        parent.VisualChildren.Add(child);
        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Contains(child, parent.VisualChildren);
        Assert.True(child.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
    }

    [Fact]
    public void PresenceCanCoexistWithLayoutMotionWithoutClearingLayoutCorrection()
    {
        ManualMotionClock clock = new();
        (UIRoot root, Canvas parent, Rectangle child) = CreateScenario(clock);
        child.LayoutMotionId = "presence-layout";
        child.LayoutMotion = Cerneala.UI.Motion.Layout.LayoutMotionOptions.Spring(MotionFactory.Tween<Transform>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        Canvas.SetLeft(child, 40);
        root.ProcessFrame();
        Assert.NotNull(root.Motion.Layout.GetBinding(child));

        parent.VisualChildren.Remove(child);

        Assert.NotNull(root.Motion.Layout.GetBinding(child));
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
    }

    private static (UIRoot Root, Canvas Parent, Rectangle Child) CreateScenario(ManualMotionClock clock)
    {
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        Rectangle child = CreateChild();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        return (root, parent, child);
    }

    private static (UIRoot Root, Canvas Parent, Rectangle Child) CreatePresentScenario(
        ManualMotionClock clock,
        bool excludeInputWhileExiting)
    {
        UIRoot root = new(100, 100, motionClock: clock);
        Canvas parent = new();
        MotionSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100));
        Rectangle child = new()
        {
            Fill = new SolidColorBrush(Color.White),
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20)),
            Presence = PresenceOptions.FadeAndScale(spec, spec, excludeInputWhileExiting)
        };
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        return (root, parent, child);
    }

    private static InputFrame PressFrame(float x, float y)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = previous.WithButton(InputMouseButton.Left, true);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static Rectangle CreateChild()
    {
        return new Rectangle
        {
            Fill = new SolidColorBrush(Color.White),
            Geometry = new RectangleGeometry(new DrawRect(0, 0, 20, 20)),
            Presence = CreatePresenceOptions()
        };
    }

    private static PresenceOptions CreatePresenceOptions()
    {
        return PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
    }
}
