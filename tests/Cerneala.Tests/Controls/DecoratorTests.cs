using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

public sealed class DecoratorTests
{
    [Fact]
    public void ChildBecomesLogicalAndVisualChild()
    {
        Decorator decorator = new();
        UIElement child = new();

        decorator.Child = child;

        Assert.Same(decorator, child.LogicalParent);
        Assert.Same(decorator, child.VisualParent);
    }

    [Fact]
    public void ReplacingChildRemovesOldChild()
    {
        Decorator decorator = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        decorator.Child = oldChild;

        decorator.Child = newChild;

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(decorator, newChild.LogicalParent);
    }

    [Fact]
    public void RejectedReparentLeavesDecoratorWithoutPartialChild()
    {
        Decorator decorator = new();
        UIElement child = new();
        UIElement visualParent = new();
        visualParent.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => decorator.Child = child);

        Assert.Null(decorator.Child);
        Assert.Null(child.LogicalParent);
        Assert.Same(visualParent, child.VisualParent);
        Assert.DoesNotContain(child, decorator.LogicalChildren);
        Assert.DoesNotContain(child, decorator.VisualChildren);
    }

    [Fact]
    public void RejectedReparentDoesNotDetachExistingAttachedChild()
    {
        UIRoot root = new();
        Decorator decorator = new();
        LifecycleElement oldChild = new();
        UIElement newChild = new();
        UIElement visualParent = new();
        root.VisualChildren.Add(decorator);
        decorator.Child = oldChild;
        visualParent.VisualChildren.Add(newChild);
        int attachedCount = oldChild.AttachedCount;
        int detachedCount = oldChild.DetachedCount;

        Assert.Throws<InvalidOperationException>(() => decorator.Child = newChild);

        Assert.Same(oldChild, decorator.Child);
        Assert.Same(decorator, oldChild.LogicalParent);
        Assert.Same(decorator, oldChild.VisualParent);
        Assert.Equal(attachedCount, oldChild.AttachedCount);
        Assert.Equal(detachedCount, oldChild.DetachedCount);
    }

    [Fact]
    public void MeasuresAndArrangesChildInsideInsets()
    {
        Decorator decorator = new()
        {
            Padding = new Thickness(1),
            // A negative side is the inward border: it takes room from the content (a positive side takes none).
            BorderThickness = new Thickness(-2)
        };
        FixedElement child = new(new LayoutSize(10, 5));
        decorator.Child = child;

        LayoutSize desired = decorator.Measure(new MeasureContext(new LayoutSize(100, 100)));
        decorator.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), child.ArrangedBounds);
    }

    // Issue #542: removing the visual child first and the logical child second canceled the exit at once.
    [Fact]
    public void ChildSetToNullKeepsTheExitOfAChildWithPresenceRunningUntilItEnds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border border = new();
        root.VisualChildren.Add(border);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        border.Child = child;
        root.ProcessFrame();

        border.Child = null;
        root.ProcessFrame();

        Assert.Null(border.Child);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal([child], root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.True(child.IsAttached);
        Assert.Null(child.LogicalParent);
        Assert.Empty(border.VisualChildren);
        Assert.Empty(border.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.False(child.IsAttached);
    }

    // Issue #542: a replaced child with presence exits while the new child becomes present.
    [Fact]
    public void ChildReplacedKeepsTheExitOfTheOldChildWithPresenceRunningAndTheNewChildIsPresent()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border border = new();
        root.VisualChildren.Add(border);
        Border oldChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        Border newChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        border.Child = oldChild;
        root.ProcessFrame();

        border.Child = newChild;
        root.ProcessFrame();

        Assert.Same(newChild, border.Child);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(oldChild));
        Assert.Equal([oldChild], root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.True(oldChild.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newChild));
        Assert.Equal([newChild], border.VisualChildren);
        Assert.Equal([newChild], border.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(oldChild));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.False(oldChild.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newChild));
    }

    // Issue #542: setting the exiting child again cancels its exit and makes it the child of both trees again.
    [Fact]
    public void ChildSetAgainWhileItExitsCancelsTheExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border border = new();
        root.VisualChildren.Add(border);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        border.Child = child;
        root.ProcessFrame();
        border.Child = null;
        root.ProcessFrame();

        border.Child = child;
        root.ProcessFrame();

        Assert.Same(child, border.Child);
        Assert.Same(border, child.LogicalParent);
        Assert.Same(border, child.VisualParent);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.True(child.IsAttached);
    }

    // A rejected child never became the child, so it leaves at once instead of exiting, and the old child stays.
    [Fact]
    public void ChildAssignmentWhoseVisualChangedHandlerThrowsDetachesTheRejectedChildWithPresenceAtOnce()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border border = new();
        root.VisualChildren.Add(border);
        Border oldChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        Border newChild = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        border.Child = oldChild;
        root.ProcessFrame();
        bool throwOnce = true;
        border.VisualChildren.Changed += (_, change) =>
        {
            if (throwOnce && change.Kind == ElementTreeChangeKind.Added && ReferenceEquals(change.Child, newChild))
            {
                throwOnce = false;
                throw new InvalidOperationException("handler failed");
            }
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => border.Child = newChild);
        root.ProcessFrame();

        Assert.Equal("handler failed", exception.Message);
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(newChild));
        Assert.False(newChild.IsAttached);
        Assert.Null(newChild.LogicalParent);
        Assert.Null(newChild.VisualParent);
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.Same(oldChild, border.Child);
        Assert.Equal([oldChild], border.VisualChildren);
        Assert.Equal([oldChild], border.LogicalChildren);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(oldChild));
    }

    // Issue #542: conditional Border content goes through Decorator.Child, so its removal must not cancel the exit.
    [Fact]
    public void ConditionalBorderChildWithPresenceExitsWhenItsConditionTurnsFalseAndReturnsWhenItTurnsTrue()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border border = new();
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(border, UIElement.IsEnabledProperty);
        using IDisposable conditions = GeneratedMarkup.AttachConditions(
            border,
            [observation],
            [new MarkupConditionRule(0, () => (bool)observation.Value!, null, new MarkupConditionalContent(0, () => [child]))]);
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        Assert.Same(child, border.Child);

        border.IsEnabled = false;
        root.ProcessFrame();

        Assert.Null(border.Child);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal([child], root.Motion.Presence.GetExitingVisualChildren(border));
        Assert.True(child.IsAttached);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.False(child.IsAttached);

        border.IsEnabled = true;
        root.ProcessFrame();

        Assert.Same(child, border.Child);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
    }

    private static PresenceOptions CreatePresence()
    {
        return PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class LifecycleElement : UIElement
    {
        public int AttachedCount { get; private set; }

        public int DetachedCount { get; private set; }

        protected override void OnAttached()
        {
            AttachedCount++;
        }

        protected override void OnDetached()
        {
            DetachedCount++;
        }
    }
}
