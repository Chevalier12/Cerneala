using Cerneala.Drawing;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Rendering;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

public sealed class TabItemTests
{
    [Fact]
    public void TabItemExposesHeaderAndSelectedState()
    {
        TabItem tabItem = new();

        tabItem.Header = "Settings";
        tabItem.IsSelected = true;

        Assert.Equal("Settings", tabItem.Header);
        Assert.True(tabItem.IsSelected);
    }

    [Fact]
    public void TabItemTracksPreparedItemContainerState()
    {
        TabControl tabControl = new();
        TabItem tabItem = new();
        tabControl.SetItems(new[] { tabItem });
        tabControl.SelectedIndex = 0;

        tabControl.ItemContainerGenerator.Realize();

        Assert.Equal(0, tabItem.ItemIndex);
        Assert.Same(tabItem, tabItem.Item);
        Assert.True(tabItem.IsSelected);
    }

    [Fact]
    public void TabItemWithoutComponentTemplateFillsItsArrangedBoundsWithBackground()
    {
        UIRoot root = new(400, 200);
        SolidColorBrush red = new(new Color(255, 0, 0));
        TabItem first = new() { Header = "Page 1", Background = red };
        TabItem second = new() { Header = "Page 2", Background = red };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { first, second });
        tabControl.SelectedIndex = 1;
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        foreach (TabItem tab in new[] { first, second })
        {
            DrawCommandList commands = Render(tab);

            DrawCommand fill = Assert.Single(commands, command => command.Kind == DrawCommandKind.FillRectangle);
            Assert.Same(red, fill.Brush);
            Assert.Equal(
                new DrawRect(tab.ArrangedBounds.X, tab.ArrangedBounds.Y, tab.ArrangedBounds.Width, tab.ArrangedBounds.Height),
                fill.Rect);
        }

        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }

    [Fact]
    public void TabItemWithoutBackgroundDrawsNoFill()
    {
        UIRoot root = new(400, 200);
        TabItem tab = new() { Header = "Page 1" };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { tab });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Empty(Render(tab));
    }

    [Fact]
    public void TabItemWithComponentTemplateLeavesItsBackgroundToTheTemplate()
    {
        UIRoot root = new(400, 200);
        TabItem tab = new()
        {
            Header = "Page 1",
            Background = new SolidColorBrush(new Color(255, 0, 0)),
            ComponentTemplate = new ComponentTemplate<TabItem>("tab", _ => new Border())
        };
        TabControl tabControl = new();
        tabControl.SetItems(new[] { tab });
        root.VisualChildren.Add(tabControl);
        root.ProcessFrame();

        Assert.Empty(Render(tab));
    }

    // Issue #627: removing the visual child first and the logical child second canceled the exit at once.
    [Fact]
    public void HeaderSetToNullKeepsTheExitOfAHeaderElementWithPresenceRunningUntilItEnds()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border header = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = header;
        root.ProcessFrame();
        Assert.Same(tab, header.VisualParent);

        tab.Header = null;
        root.ProcessFrame();

        Assert.Null(tab.Header);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(header));
        Assert.Equal([header], root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.True(header.IsAttached);
        Assert.Null(header.LogicalParent);
        Assert.Empty(tab.VisualChildren);
        Assert.Empty(tab.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(header));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.False(header.IsAttached);
    }

    // Issue #627: a replaced header element with presence exits while the new header element becomes present.
    [Fact]
    public void HeaderReplacedKeepsTheExitOfTheOldHeaderElementWithPresenceRunningAndTheNewElementIsPresent()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border oldHeader = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        Border newHeader = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = oldHeader;
        root.ProcessFrame();

        tab.Header = newHeader;
        root.ProcessFrame();

        Assert.Same(newHeader, tab.Header);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(oldHeader));
        Assert.Equal([oldHeader], root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.True(oldHeader.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newHeader));
        Assert.Equal([newHeader], tab.VisualChildren);
        Assert.Equal([newHeader], tab.LogicalChildren);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(oldHeader));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.False(oldHeader.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(newHeader));
    }

    // Issue #627: setting the exiting element as Header again cancels its exit and hosts it in both trees again.
    [Fact]
    public void HeaderSetAgainWhileItsElementExitsCancelsTheExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border header = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = header;
        root.ProcessFrame();
        tab.Header = null;
        root.ProcessFrame();

        tab.Header = header;
        root.ProcessFrame();

        Assert.Same(header, tab.Header);
        Assert.Same(tab, header.LogicalParent);
        Assert.Same(tab, header.VisualParent);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(header));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.True(header.IsAttached);
    }

    // Issue #627: a failed replacement restores the previous header element without an exit and removes the rejected
    // element at once.
    [Fact]
    public void FailedHeaderReplacementRestoresThePreviousElementWithPresenceAndRemovesTheRejectedElementAtOnce()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border oldHeader = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = oldHeader;
        root.ProcessFrame();
        Button failed = new() { Content = "next", Presence = CreatePresence() };
        failed.Loaded += (_, _) => throw new InvalidOperationException("Loaded handler failed");

        Assert.Throws<InvalidOperationException>(() => tab.Header = failed);
        root.ProcessFrame();

        Assert.Same(oldHeader, tab.Header);
        Assert.Same(tab, oldHeader.LogicalParent);
        Assert.Same(tab, oldHeader.VisualParent);
        Assert.Equal([oldHeader], tab.VisualChildren);
        Assert.Equal([oldHeader], tab.LogicalChildren);
        Assert.Null(failed.LogicalParent);
        Assert.Null(failed.VisualParent);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(oldHeader));
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(failed));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.True(oldHeader.IsAttached);
        Assert.False(failed.IsAttached);
    }

    // Issue #627: the header element leaves the tab for the header presenter when a HeaderTemplate is set. That is not an
    // exit: a template that does not show the element removes it at once, as it did before.
    [Fact]
    public void HeaderElementReleasedForAHeaderTemplateThatDoesNotShowItLeavesWithoutAnExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border header = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = header;
        root.ProcessFrame();

        tab.HeaderTemplate = new ContentTemplate<object>(
            "header",
            key: null,
            priority: 0,
            _ => new Border { Width = 30, Height = 10 });
        root.ProcessFrame();

        Assert.Same(header, tab.Header);
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(header));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.False(header.IsAttached);
    }

    // Issue #627: Header changed through SetValue, not the setter, also lets the old header element exit.
    [Fact]
    public void HeaderChangedThroughSetValueKeepsTheExitOfTheOldHeaderElement()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border header = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = header;
        root.ProcessFrame();

        tab.SetValue(TabItem.HeaderProperty, "Text header");
        root.ProcessFrame();

        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(header));
        Assert.Equal([header], root.Motion.Presence.GetExitingVisualChildren(tab));
        Assert.Null(header.LogicalParent);

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(header));
        Assert.False(header.IsAttached);
    }

    // Issue #627: a header element that moves from the tab into the header presenter, because a HeaderTemplate is set
    // that shows the element, stays present and does not exit.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderElementThatMovesToTheHeaderPresenterDoesNotExit(bool templateWrapsTheElement)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        TabItem tab = new();
        root.VisualChildren.Add(tab);
        Border header = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        tab.Header = header;
        root.ProcessFrame();

        tab.HeaderTemplate = new ContentTemplate<UIElement>(
            "header",
            key: null,
            priority: 0,
            context => templateWrapsTheElement ? new Border { Child = context.Data } : context.Data);
        root.ProcessFrame();

        Assert.Same(header, tab.Header);
        Assert.NotSame(tab, header.VisualParent);
        Assert.True(header.IsAttached);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(header));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
    }

    // Issue #627: a header element that a presenter's next template takes from a tab of its previous template is released
    // for that new owner. That is not an exit, so the new template can add it to the element it creates.
    [Fact]
    public void HeaderElementReleasedForANewOwnerDoesNotExit()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(200, 200, motionClock: clock);
        Border header = new() { Width = 50, Height = 20, Presence = CreatePresence() };
        ContentPresenter presenter = new()
        {
            Content = header,
            ContentTemplate = new ContentTemplate<UIElement>("tab", key: null, priority: 0, context => new TabItem { Header = context.Data })
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        TabItem tab = Assert.IsType<TabItem>(header.VisualParent);

        presenter.ContentTemplate = new ContentTemplate<UIElement>("wrap", key: null, priority: 0, context => new Border { Child = context.Data });
        root.ProcessFrame();

        Border wrapper = Assert.IsType<Border>(header.VisualParent);
        Assert.Same(header, wrapper.Child);
        Assert.Null(tab.Header);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(header));
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(tab));
    }

    private static PresenceOptions CreatePresence()
    {
        return PresenceOptions.FadeAndScale(
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
    }

    private static DrawCommandList Render(UIElement element)
    {
        DrawCommandList commands = new();
        element.Render(new RenderContext(element, new DrawingContext(commands), element.ArrangedBounds, RenderLayer.Default, new RenderCounters()));
        return commands;
    }
}
