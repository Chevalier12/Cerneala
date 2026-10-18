using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using LayoutPanel = Cerneala.UI.Layout.Panels.Panel;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Layout;

// Issue #405: one public call adds a panel child to both trees, as WPF Panel.Children does.
public sealed class PanelChildrenTests
{
    public static TheoryData<string> Panels => new() { nameof(StackPanel), nameof(Cerneala.UI.Layout.Panels.Grid) };

    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenAddMakesTheChildALogicalAndVisualChildThatThePanelLaysOut(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border child = new() { Width = 50, Height = 20 };

        panel.Children.Add(child);
        panel.Measure(new MeasureContext(new LayoutSize(200, 200), default));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200), default));

        Assert.Same(panel, child.LogicalParent);
        Assert.Same(panel, child.VisualParent);
        Assert.Equal(1, panel.Children.Count);
        Assert.Same(child, panel.Children[0]);
        if (panelType == nameof(StackPanel))
        {
            Assert.Equal(new LayoutSize(50, 20), panel.DesiredSize);
            Assert.Equal(new LayoutRect(75, 0, 50, 20), child.ArrangedBounds);
        }
        else
        {
            // A grid without definitions has one star row and column that take the finite available size.
            Assert.Equal(new LayoutSize(200, 200), panel.DesiredSize);
            Assert.Equal(new LayoutRect(75, 90, 50, 20), child.ArrangedBounds);
        }
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenRemoveClearsBothParents(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border child = new() { Width = 50, Height = 20 };
        panel.Children.Add(child);

        bool removed = panel.Children.Remove(child);

        Assert.True(removed);
        Assert.Null(child.LogicalParent);
        Assert.Null(child.VisualParent);
        Assert.Equal(0, panel.Children.Count);
        Assert.Empty(panel.LogicalChildren);
        Assert.Empty(panel.VisualChildren);
        Assert.False(panel.Children.Remove(child));
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenInsertKeepsTheVisualAndLogicalOrder(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border first = new();
        Border second = new();
        panel.Children.Add(first);

        panel.Children.Insert(0, second);

        Assert.Equal([second, first], panel.Children);
        Assert.Equal([second, first], panel.VisualChildren);
        Assert.Equal([second, first], panel.LogicalChildren);
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void LogicalChildrenAddOfAChildThatIsNotAVisualChildThrowsAndNamesChildren(string panelType)
    {
        LayoutPanel panel = CreatePanel(panelType);
        Border child = new() { Width = 50, Height = 20 };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => panel.LogicalChildren.Add(child));

        Assert.Contains("Panel.Children", exception.Message, StringComparison.Ordinal);
        Assert.Null(child.LogicalParent);
        Assert.Empty(panel.LogicalChildren);
    }

    [Fact]
    public void LogicalChildrenAddOfAVisualChildOfThePanelIsAllowed()
    {
        StackPanel panel = new();
        Border child = new();
        panel.VisualChildren.Add(child);

        panel.LogicalChildren.Add(child);

        Assert.Same(panel, child.LogicalParent);
        Assert.Same(panel, child.VisualParent);
    }

    // Issue #493: removing the visual child first and the logical child second canceled the exit at once.
    [Theory]
    [MemberData(nameof(Panels))]
    public void ChildrenRemoveOfAChildWithPresenceKeepsTheExitRunningUntilItEnds(string panelType)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        LayoutPanel panel = CreatePanel(panelType);
        root.VisualChildren.Add(panel);
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        panel.Children.Add(child);
        root.ProcessFrame();

        Assert.True(panel.Children.Remove(child));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal([child], root.Motion.Presence.GetExitingVisualChildren(panel));
        Assert.True(child.IsAttached);
        Assert.Empty(panel.Children);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(panel));
        Assert.False(child.IsAttached);
    }

    // Issue #493: conditional panel children are reconciled visual first for insertion; removal must not cancel the exit.
    [Fact]
    public void ConditionalPanelChildWithPresenceExitsWhenItsConditionTurnsFalseAndReturnsWhenItTurnsTrue()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        StackPanel panel = new();
        Border child = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(panel, UIElement.IsEnabledProperty);
        using IDisposable conditions = GeneratedMarkup.AttachConditions(
            panel,
            [observation],
            [new MarkupConditionRule(0, () => (bool)observation.Value!, null, new MarkupConditionalContent(0, () => [child]))]);
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        Assert.Equal([child], panel.Children);
        Assert.Equal([child], panel.LogicalChildren);

        panel.IsEnabled = false;
        root.ProcessFrame();

        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(child));
        Assert.Equal([child], root.Motion.Presence.GetExitingVisualChildren(panel));
        Assert.True(child.IsAttached);
        Assert.Empty(panel.Children);
        Assert.Empty(panel.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(child));
        Assert.False(child.IsAttached);

        panel.IsEnabled = true;
        root.ProcessFrame();

        Assert.Equal([child], panel.Children);
        Assert.Equal([child], panel.LogicalChildren);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(child));
    }

    private static PresenceOptions CreatePresence()
    {
        return PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
    }

    private static LayoutPanel CreatePanel(string panelType)
    {
        return panelType switch
        {
            nameof(StackPanel) => new StackPanel(),
            _ => new Cerneala.UI.Layout.Panels.Grid()
        };
    }
}
