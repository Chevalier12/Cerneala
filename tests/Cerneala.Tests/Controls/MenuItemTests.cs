using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;
using LayoutGrid = Cerneala.UI.Layout.Panels.Grid;
using LayoutStackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.Controls;

public sealed class MenuItemTests
{
    [Fact]
    public void DefaultsExposeThePlannedInputAndItemsContracts()
    {
        MenuItem item = new();

        Assert.IsAssignableFrom<ItemsControl>(item);
        Assert.IsAssignableFrom<IInputCommandSource>(item);
        Assert.IsAssignableFrom<ICommandStateSource>(item);
        Assert.IsAssignableFrom<IInputActivatable>(item);
        Assert.Null(item.Header);
        Assert.Null(item.Command);
        Assert.Null(item.CommandParameter);
        Assert.False(item.IsSubmenuOpen);
        Assert.NotNull(item.ComponentTemplate);
        Assert.Equal(Orientation.Vertical, Assert.IsType<LayoutStackPanel>(item.ItemsPanel).Orientation);
    }

    [Fact]
    public void DataChildrenUseMenuItemContainersAndDisplayMemberPathForHeaders()
    {
        MenuItem owner = new() { DisplayMemberPath = nameof(MenuEntry.Label) };
        MenuItem explicitContainer = new() { Header = "Explicit" };
        owner.Items.Add(new MenuEntry("Generated", "Alternate"));
        owner.Items.Add(explicitContainer);

        MenuItem generated = Assert.IsType<MenuItem>(owner.ItemContainerGenerator.GetOrCreate(0));
        UIElement direct = owner.ItemContainerGenerator.GetOrCreate(1);

        Assert.Equal("Generated", generated.Header);
        Assert.Same(explicitContainer, direct);
        Assert.Equal("Explicit", explicitContainer.Header);
    }

    [Fact]
    public void HeaderIsPresentedAndTracksChanges()
    {
        MenuItem item = new() { Header = "File" };

        item.ApplyTemplate();
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(
            item.ComponentTemplateInstance!.Parts["PART_HeaderPresenter"]);
        Assert.Equal("File", presenter.Content);

        item.Header = "Edit";

        Assert.Equal("Edit", presenter.Content);
    }

    [Fact]
    public void ParentActivationOpensSubmenuAndNeverExecutesCommand()
    {
        int executions = 0;
        MenuItem parent = new()
        {
            Header = "File",
            Command = new ActionCommand(_ => executions++)
        };
        parent.Items.Add("Open");
        UIRoot root = Attach(parent);
        ElementInputRouteMap routes = root.InputCache.EnsureCurrent(root);

        ((IInputActivatable)parent).Activate();
        bool executed = parent.ExecuteCommand(new CommandRouter(), routes);

        Assert.True(parent.IsSubmenuOpen);
        Assert.False(executed);
        Assert.Equal(0, executions);
    }

    [Fact]
    public void LeafPointerActivationRaisesClickThenExecutesCommandExactlyOnce()
    {
        List<string> calls = [];
        MenuItem leaf = new()
        {
            Header = "Open",
            Width = 100,
            Height = 40,
            Command = new ActionCommand(_ => calls.Add("command"))
        };
        leaf.Click += (_, _) => calls.Add("click");
        UIRoot root = Attach(leaf);
        ElementInputBridge bridge = new();
        float x = leaf.ArrangedBounds.X + 10;
        float y = leaf.ArrangedBounds.Y + 10;
        HitTestResult hit = Assert.IsType<HitTestResult>(new HitTestService().HitTest(root, x, y));
        Assert.True(IsVisualDescendantOf(hit.Element, leaf), $"Hit '{hit.Element.GetType().Name}' outside MenuItem bounds {leaf.ArrangedBounds}.");

        Click(bridge, root, x, y);

        Assert.Equal(["click", "command"], calls);
        Assert.False(leaf.IsSubmenuOpen);
    }

    [Fact]
    public void SubmenuStateAndOverlaySynchronizeBidirectionallyWithSingleEvents()
    {
        MenuItem parent = new() { Header = "File" };
        parent.Items.Add("Open");
        Attach(parent);
        Overlay overlay = Assert.IsType<Overlay>(
            parent.ComponentTemplateInstance!.Parts["PART_SubmenuOverlay"]);
        int opened = 0;
        int closed = 0;
        parent.SubmenuOpened += (_, _) => opened++;
        parent.SubmenuClosed += (_, _) => closed++;

        parent.IsSubmenuOpen = true;
        parent.IsSubmenuOpen = true;
        Assert.True(overlay.IsOpen);
        Assert.Equal(1, opened);

        overlay.IsOpen = false;
        Assert.False(parent.IsSubmenuOpen);
        Assert.Equal(1, closed);

        overlay.IsOpen = true;
        Assert.True(parent.IsSubmenuOpen);
        Assert.Equal(2, opened);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void DisableChildRemovalAndDetachCloseSubmenu()
    {
        MenuItem parent = new() { Header = "File" };
        parent.Items.Add("Open");
        UIRoot root = Attach(parent);

        parent.IsSubmenuOpen = true;
        parent.IsEnabled = false;
        Assert.False(parent.IsSubmenuOpen);

        parent.IsEnabled = true;
        parent.IsSubmenuOpen = true;
        parent.Items.Clear();
        Assert.False(parent.IsSubmenuOpen);

        parent.Items.Add("Open");
        parent.IsSubmenuOpen = true;
        root.VisualChildren.Remove(parent);
        Assert.False(parent.IsSubmenuOpen);
    }

    [Fact]
    public void TemplateSwapClosesOldOverlayAndDetachesItsHandlers()
    {
        MenuItem parent = new() { Header = "File" };
        parent.Items.Add("Open");
        Attach(parent);
        parent.IsSubmenuOpen = true;
        Overlay oldOverlay = Assert.IsType<Overlay>(
            parent.ComponentTemplateInstance!.Parts["PART_SubmenuOverlay"]);
        int opened = 0;
        int closed = 0;
        parent.SubmenuOpened += (_, _) => opened++;
        parent.SubmenuClosed += (_, _) => closed++;

        parent.ComponentTemplate = CreateTemplate("replacement");

        Overlay replacement = Assert.IsType<Overlay>(
            parent.ComponentTemplateInstance!.Parts["PART_SubmenuOverlay"]);
        Assert.False(parent.IsSubmenuOpen);
        Assert.False(oldOverlay.IsOpen);
        Assert.False(replacement.IsOpen);
        Assert.Equal(1, closed);

        oldOverlay.RaiseEvent(new RoutedEventArgs(Overlay.OpenedEvent, oldOverlay));
        Assert.False(parent.IsSubmenuOpen);
        Assert.Equal(0, opened);

        parent.IsSubmenuOpen = true;
        parent.IsSubmenuOpen = false;
        Assert.Equal(1, opened);
        Assert.Equal(2, closed);
    }

    [Fact]
    public void ReattachDoesNotDuplicateOverlayHandlers()
    {
        MenuItem parent = new() { Header = "File" };
        parent.Items.Add("Open");
        UIRoot root = Attach(parent);
        int opened = 0;
        int closed = 0;
        parent.SubmenuOpened += (_, _) => opened++;
        parent.SubmenuClosed += (_, _) => closed++;

        root.VisualChildren.Remove(parent);
        root.VisualChildren.Add(parent);
        root.ProcessFrame();
        parent.IsSubmenuOpen = true;
        parent.IsSubmenuOpen = false;

        Assert.Equal(1, opened);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void ItemsSourceAndDisplayMemberPathChangesRebuildGeneratedHeaders()
    {
        ObservableList<MenuEntry> source = new([new MenuEntry("One", "First")]);
        MenuItem owner = new()
        {
            DisplayMemberPath = nameof(MenuEntry.Label),
            ItemsSource = source
        };
        Attach(owner);

        MenuItem first = Assert.IsType<MenuItem>(owner.ItemContainerGenerator.GetOrCreate(0));
        Assert.Equal("One", first.Header);

        owner.DisplayMemberPath = nameof(MenuEntry.Alternate);
        MenuItem rebuilt = Assert.IsType<MenuItem>(owner.ItemContainerGenerator.GetOrCreate(0));
        Assert.Equal("First", rebuilt.Header);

        source.Add(new MenuEntry("Two", "Second"));
        MenuItem added = Assert.IsType<MenuItem>(owner.ItemContainerGenerator.GetOrCreate(1));
        Assert.Equal("Second", added.Header);
    }

    private static UIRoot Attach(MenuItem item)
    {
        UIRoot root = new(240, 160);
        root.VisualChildren.Add(item);
        root.ProcessFrame();
        return root;
    }

    private static void Click(ElementInputBridge bridge, UIRoot root, float x, float y)
    {
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
    }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
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

    private static ComponentTemplate<MenuItem> CreateTemplate(string key)
    {
        return new ComponentTemplate<MenuItem>(key, context =>
        {
            ContentPresenter header = new();
            ItemsPresenter items = new();
            Overlay overlay = new()
            {
                Content = items,
                Placement = OverlayPlacement.AutoHorizontal
            };
            LayoutGrid root = new();
            root.VisualChildren.Add(header);
            root.VisualChildren.Add(overlay);
            context.RequirePart("PART_HeaderPresenter", header);
            context.RequirePart("PART_SubmenuOverlay", overlay);
            context.RequirePart("PART_ItemsPresenter", items);
            return root;
        });
    }

    private static bool IsVisualDescendantOf(UIElement element, UIElement ancestor)
    {
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record MenuEntry(string Label, string Alternate);
}
