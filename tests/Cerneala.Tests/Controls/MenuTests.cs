using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;
using LayoutStackPanel = Cerneala.UI.Layout.Panels.StackPanel;

namespace Cerneala.Tests.Controls;

public sealed class MenuTests
{
    [Fact]
    public void MenuDefaultsToVerticalItemsAndBuildsMenuItemContainersAcrossThreeLevels()
    {
        Menu menu = new() { DisplayMemberPath = nameof(MenuEntry.Label) };
        MenuItem rootParent = Branch("File", Branch("Recent", Branch("Pinned", Leaf("One"))));
        menu.Items.Add(new MenuEntry("Generated"));
        menu.Items.Add(rootParent);

        UIRoot root = Attach(menu);
        MenuItem generated = Container(menu, 0);
        MenuItem direct = Container(menu, 1);

        Assert.IsAssignableFrom<ItemsControl>(menu);
        Assert.Equal(Orientation.Vertical, Assert.IsType<LayoutStackPanel>(menu.ItemsPanel).Orientation);
        Assert.NotNull(menu.ComponentTemplateInstance!.Parts["PART_ItemsPresenter"]);
        Assert.Equal("Generated", generated.Header);
        Assert.Same(rootParent, direct);

        direct.IsSubmenuOpen = true;
        root.ProcessFrame();
        MenuItem recent = Container(direct, 0);
        recent.IsSubmenuOpen = true;
        root.ProcessFrame();
        MenuItem pinned = Container(recent, 0);
        pinned.IsSubmenuOpen = true;
        root.ProcessFrame();

        Assert.True(direct.IsSubmenuOpen);
        Assert.True(recent.IsSubmenuOpen);
        Assert.True(pinned.IsSubmenuOpen);
        Assert.Equal(3, menu.Session.OpenPath.Count);
    }

    [Fact]
    public void MenuBarUsesHorizontalRootAndHoverMovesTheOnlyOpenBranch()
    {
        MenuItem file = Branch("File", Leaf("Open"));
        MenuItem edit = Branch("Edit", Leaf("Copy"));
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        menuBar.Items.Add(edit);
        UIRoot root = Attach(menuBar, 320, 180);
        ElementInputBridge bridge = new();

        Assert.Equal(Orientation.Horizontal, Assert.IsType<LayoutStackPanel>(menuBar.ItemsPanel).Orientation);
        Click(bridge, root, file);
        root.ProcessFrame();
        MovePointer(bridge, root, edit);
        root.ProcessFrame();

        Assert.False(file.IsSubmenuOpen);
        Assert.True(edit.IsSubmenuOpen);
        Assert.Single(menuBar.Session.OpenPath);
        Assert.Same(edit, menuBar.Session.OpenPath[0]);
    }

    [Fact]
    public void MenuBarRootItemBoundsRemainStableWhenFocusMovesIntoSubmenu()
    {
        MenuItem file = new()
        {
            Header = "File",
            Padding = new Thickness(16, 16, 16, 15)
        };
        file.Items.Add(Leaf("Open"));
        MenuItem edit = new()
        {
            Header = "Edit",
            Padding = new Thickness(16, 16, 16, 15)
        };
        edit.Items.Add(Leaf("Copy"));
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        menuBar.Items.Add(edit);
        UIRoot root = new(320, 180);
        root.AspectRegistry.Register(
            AspectPackage.Create("Menu test").Components(components => components.AddRule(
                new AspectRuleSet(
                    "menu-item-font",
                    AspectLayer.App,
                    new AspectTarget(typeof(MenuItem)),
                    [
                        new AspectDeclaration(
                            Control.FontFamilyProperty,
                            AspectValue<string>.Literal("Segoe UI Semibold")),
                        new AspectDeclaration(Control.FontSizeProperty, AspectValue<float>.Literal(13))
                    ],
                    0))));
        root.VisualChildren.Add(menuBar);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        LayoutRect fileBounds = file.ArrangedBounds;
        LayoutRect editBounds = edit.ArrangedBounds;

        Click(bridge, root, file);
        root.ProcessFrame();
        Press(bridge, root, InputKey.Down);
        root.ProcessFrame();

        Assert.Equal(fileBounds, file.ArrangedBounds);
        Assert.Equal(editBounds, edit.ArrangedBounds);
    }

    [Fact]
    public void VerticalHoverOpensParentAndClosesTheSiblingBranch()
    {
        MenuItem first = Branch("First", Leaf("One"));
        MenuItem second = Branch("Second", Leaf("Two"));
        Menu menu = new();
        menu.Items.Add(first);
        menu.Items.Add(second);
        UIRoot root = Attach(menu);
        ElementInputBridge bridge = new();

        MovePointer(bridge, root, first);
        root.ProcessFrame();
        MovePointer(bridge, root, second);
        root.ProcessFrame();

        Assert.False(first.IsSubmenuOpen);
        Assert.True(second.IsSubmenuOpen);
        Assert.Single(menu.Session.OpenPath);
        Assert.Same(second, menu.Session.OpenPath[0]);
    }

    [Fact]
    public void LeafPointerActivationRaisesAndExecutesOnceThenClosesWholeSession()
    {
        int clicks = 0;
        int executions = 0;
        MenuItem leaf = Leaf("Open");
        leaf.Click += (_, _) => clicks++;
        leaf.Command = new ActionCommand(_ => executions++);
        MenuItem file = Branch("File", leaf);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        UIRoot root = Attach(menuBar, 320, 180);
        ElementInputBridge bridge = new();

        Click(bridge, root, file);
        root.ProcessFrame();
        Click(bridge, root, leaf);

        Assert.Equal(1, clicks);
        Assert.Equal(1, executions);
        Assert.False(file.IsSubmenuOpen);
        Assert.Empty(menuBar.Session.OpenPath);
    }

    [Fact]
    public void VerticalKeyboardNavigationSkipsDisabledAndCollapsedItemsWithoutWrapping()
    {
        MenuItem first = Leaf("First");
        MenuItem disabled = Leaf("Disabled");
        disabled.IsEnabled = false;
        MenuItem collapsed = Leaf("Collapsed");
        collapsed.Visibility = Visibility.Collapsed;
        MenuItem last = Leaf("Last");
        Menu menu = new();
        menu.Items.Add(first);
        menu.Items.Add(disabled);
        menu.Items.Add(collapsed);
        menu.Items.Add(last);
        UIRoot root = Attach(menu);
        ElementInputBridge bridge = FocusedBridge(root, first);

        Press(bridge, root, InputKey.Down);
        Assert.Same(last, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Down);
        Assert.Same(last, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Home);
        Assert.Same(first, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.End);
        Assert.Same(last, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Up);
        Assert.Same(first, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void MenuBarLeftAndRightWrapAndDownEntersFirstEligibleChild()
    {
        MenuItem firstChild = Leaf("Disabled");
        firstChild.IsEnabled = false;
        MenuItem eligibleChild = Leaf("Open");
        MenuItem file = Branch("File", firstChild, eligibleChild);
        MenuItem edit = Branch("Edit", Leaf("Copy"));
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        menuBar.Items.Add(edit);
        UIRoot root = Attach(menuBar);
        ElementInputBridge bridge = FocusedBridge(root, edit);

        Press(bridge, root, InputKey.Right);
        Assert.Same(file, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Left);
        Assert.Same(edit, bridge.FocusManager.FocusedElement);

        _ = bridge.FocusManager.Focus(file, root.InputCache.EnsureCurrent(root));
        Assert.Same(file, bridge.FocusManager.FocusedElement);
        Press(bridge, root, InputKey.Down);
        root.ProcessFrame();

        Assert.True(file.IsSubmenuOpen);
        Assert.Same(eligibleChild, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void RightAndLeftTraverseThreeSubmenuLevelsAndEscapeClosesTheRootLevel()
    {
        MenuItem leaf = Leaf("Leaf");
        MenuItem third = Branch("Third", leaf);
        MenuItem second = Branch("Second", third);
        MenuItem first = Branch("First", second);
        MenuBar menuBar = new();
        menuBar.Items.Add(first);
        UIRoot root = Attach(menuBar);
        ElementInputBridge bridge = FocusedBridge(root, first);

        Press(bridge, root, InputKey.Down);
        root.ProcessFrame();
        Assert.Same(second, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Right);
        root.ProcessFrame();
        Assert.Same(third, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Right);
        root.ProcessFrame();
        Assert.Same(leaf, bridge.FocusManager.FocusedElement);
        Assert.Equal([first, second, third], menuBar.Session.OpenPath);

        Press(bridge, root, InputKey.Left);
        Assert.False(third.IsSubmenuOpen);
        Assert.Same(third, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Left);
        Assert.False(second.IsSubmenuOpen);
        Assert.Same(second, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Escape);
        Assert.False(first.IsSubmenuOpen);
        Assert.Same(first, bridge.FocusManager.FocusedElement);
        Assert.Empty(menuBar.Session.OpenPath);
    }

    [Fact]
    public void EnterAndSpaceOpenParentsButLeafActivationIsNotDuplicated()
    {
        int clicks = 0;
        int commands = 0;
        MenuItem leaf = Leaf("Open");
        leaf.Click += (_, _) => clicks++;
        leaf.Command = new ActionCommand(_ => commands++);
        MenuItem file = Branch("File", leaf);
        Menu menu = new();
        menu.Items.Add(file);
        UIRoot root = Attach(menu);
        ElementInputBridge bridge = FocusedBridge(root, file);

        Press(bridge, root, InputKey.Enter);
        root.ProcessFrame();
        Assert.True(file.IsSubmenuOpen);
        Assert.Same(leaf, bridge.FocusManager.FocusedElement);

        Press(bridge, root, InputKey.Enter);
        Assert.Equal(1, clicks);
        Assert.Equal(1, commands);
        Assert.False(file.IsSubmenuOpen);

        _ = bridge.FocusManager.Focus(file, root.InputCache.EnsureCurrent(root));
        Assert.Same(file, bridge.FocusManager.FocusedElement);
        Press(bridge, root, InputKey.Space);
        root.ProcessFrame();
        Assert.True(file.IsSubmenuOpen);
        Assert.Same(leaf, bridge.FocusManager.FocusedElement);

        Release(bridge, root, InputKey.Space);
        Assert.Equal(1, clicks);
        Assert.Equal(1, commands);

        Press(bridge, root, InputKey.Space);
        Release(bridge, root, InputKey.Space);

        Assert.Equal(2, clicks);
        Assert.Equal(2, commands);
        Assert.False(file.IsSubmenuOpen);
        Assert.Empty(menu.Session.OpenPath);
    }

    [Fact]
    public void NoEligibleDestinationLeavesKeyboardFocusStable()
    {
        MenuItem parent = Branch("Parent", Leaf("Hidden"));
        ((MenuItem)parent.Items[0]!).Visibility = Visibility.Collapsed;
        Menu menu = new();
        menu.Items.Add(parent);
        UIRoot root = Attach(menu);
        ElementInputBridge bridge = FocusedBridge(root, parent);

        Press(bridge, root, InputKey.Right);
        root.ProcessFrame();

        Assert.True(parent.IsSubmenuOpen);
        Assert.Same(parent, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void LightDismissClosesEveryOverlayAndRestoresTheOpeningRootItem()
    {
        MenuItem leaf = Leaf("Leaf");
        MenuItem nested = Branch("Nested", leaf);
        MenuItem file = Branch("File", nested);
        MenuBar menuBar = new() { Width = 220 };
        menuBar.Items.Add(file);
        Button outside = new() { Content = "Outside", Width = 90, Height = 30 };
        LayoutCanvas canvas = new();
        LayoutCanvas.SetLeft(outside, 320);
        LayoutCanvas.SetTop(outside, 190);
        canvas.VisualChildren.Add(menuBar);
        canvas.VisualChildren.Add(outside);
        UIRoot root = Attach(canvas, 440, 260);
        ElementInputBridge bridge = new();

        Click(bridge, root, file);
        root.ProcessFrame();
        nested.IsSubmenuOpen = true;
        root.ProcessFrame();
        Assert.True(bridge.FocusManager.Focus(leaf, root.InputCache.EnsureCurrent(root)));

        (float outsideX, float outsideY) = PointInside(outside);
        HitTestResult outsideHit = Assert.IsType<HitTestResult>(new HitTestService().HitTest(root, outsideX, outsideY));
        Assert.True(
            IsVisualDescendantOf(outsideHit.Element, outside),
            $"Hit '{outsideHit.Element.GetType().Name}' at ({outsideX}, {outsideY}); outside={outside.ArrangedBounds}, " +
            $"fileOverlay={file.SubmenuOverlay!.ProjectedPresenter.ArrangedBounds}, nestedOverlay={nested.SubmenuOverlay!.ProjectedPresenter.ArrangedBounds}.");
        Click(bridge, root, outside);

        Assert.False(file.IsSubmenuOpen);
        Assert.False(nested.IsSubmenuOpen);
        Assert.Empty(menuBar.Session.OpenPath);
        Assert.Same(file, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void TabClosesTheSessionAndRemainsUnhandledForNormalFocusNavigation()
    {
        MenuItem leaf = Leaf("Leaf");
        MenuItem file = Branch("File", leaf);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        Button after = new() { Content = "After" };
        LayoutStackPanel panel = new();
        panel.VisualChildren.Add(menuBar);
        panel.VisualChildren.Add(after);
        UIRoot root = Attach(panel);
        ElementInputBridge bridge = FocusedBridge(root, file);

        Press(bridge, root, InputKey.Down);
        root.ProcessFrame();
        bool handled = true;
        leaf.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) => handled = args.Handled, handledEventsToo: true);

        Press(bridge, root, InputKey.Tab);

        Assert.False(handled);
        Assert.False(file.IsSubmenuOpen);
        Assert.Empty(menuBar.Session.OpenPath);
        Assert.NotSame(leaf, bridge.FocusManager.FocusedElement);
    }

    [Fact]
    public void DisableDetachAndRootLossCloseTheWholeSession()
    {
        MenuItem nested = Branch("Nested", Leaf("Leaf"));
        MenuItem file = Branch("File", nested);
        MenuBar menuBar = new();
        menuBar.Items.Add(file);
        UIRoot root = Attach(menuBar);

        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        nested.IsSubmenuOpen = true;
        root.ProcessFrame();
        menuBar.IsEnabled = false;

        Assert.False(file.IsSubmenuOpen);
        Assert.False(nested.IsSubmenuOpen);
        Assert.Empty(menuBar.Session.OpenPath);

        menuBar.IsEnabled = true;
        file.IsSubmenuOpen = true;
        root.ProcessFrame();
        root.VisualChildren.Remove(menuBar);

        Assert.False(file.IsSubmenuOpen);
        Assert.Empty(menuBar.Session.OpenPath);
    }

    [Fact]
    public void RemovingOpenItemsAndChangingItemsSourceCloseStaleBranches()
    {
        MenuItem first = Branch("First", Leaf("Leaf"));
        Menu menu = new();
        menu.Items.Add(first);
        UIRoot root = Attach(menu);

        first.IsSubmenuOpen = true;
        root.ProcessFrame();
        menu.Items.Remove(first);

        Assert.False(first.IsSubmenuOpen);
        Assert.Empty(menu.Session.OpenPath);

        MenuItem second = Branch("Second", Leaf("Leaf"));
        ObservableList<MenuItem> source = new([second]);
        menu.ItemsSource = source;
        root.ProcessFrame();
        second.IsSubmenuOpen = true;
        root.ProcessFrame();
        source.Clear();

        Assert.False(second.IsSubmenuOpen);
        Assert.Empty(menu.Session.OpenPath);
    }

    [Fact]
    public void MenuItemCanMoveBetweenRootsWithoutKeepingTheOldSession()
    {
        MenuItem shared = Branch("Shared", Leaf("Leaf"));
        Menu first = new();
        Menu second = new();
        first.Items.Add(shared);
        LayoutStackPanel panel = new();
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        UIRoot root = Attach(panel);

        shared.IsSubmenuOpen = true;
        root.ProcessFrame();
        first.Items.Remove(shared);
        second.Items.Add(shared);
        root.ProcessFrame();

        Assert.False(shared.IsSubmenuOpen);
        Assert.Empty(first.Session.OpenPath);
        Assert.Same(second.Session, shared.Session);

        shared.IsSubmenuOpen = true;
        root.ProcessFrame();
        Assert.Single(second.Session.OpenPath);
        Assert.Same(shared, second.Session.OpenPath[0]);
    }

    [Fact]
    public void ProgrammaticSiblingOpenKeepsOneRootBranchAndSharesOneDismissScope()
    {
        MenuItem first = Branch("First", Leaf("One"));
        MenuItem second = Branch("Second", Leaf("Two"));
        MenuBar menuBar = new();
        menuBar.Items.Add(first);
        menuBar.Items.Add(second);
        UIRoot root = Attach(menuBar);

        first.IsSubmenuOpen = true;
        root.ProcessFrame();
        second.IsSubmenuOpen = true;
        root.ProcessFrame();

        Assert.False(first.IsSubmenuOpen);
        Assert.True(second.IsSubmenuOpen);
        Assert.Same(first.SubmenuOverlay!.DismissScope, second.SubmenuOverlay!.DismissScope);
        Assert.Same(menuBar.Session.DismissScope, second.SubmenuOverlay.DismissScope);
    }

    private static MenuItem Branch(string header, params MenuItem[] children)
    {
        MenuItem item = new() { Header = header, Width = 90, Height = 28 };
        foreach (MenuItem child in children)
        {
            item.Items.Add(child);
        }

        return item;
    }

    private static MenuItem Leaf(string header)
    {
        return new MenuItem { Header = header, Width = 90, Height = 28 };
    }

    private static MenuItem Container(ItemsControl owner, int index)
    {
        return Assert.IsType<MenuItem>(owner.ItemContainerGenerator.GetOrCreate(index));
    }

    private static UIRoot Attach(UIElement element, float width = 360, float height = 240)
    {
        UIRoot root = new(width, height);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        return root;
    }

    private static ElementInputBridge FocusedBridge(UIRoot root, MenuItem item)
    {
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(item, root.InputCache.EnsureCurrent(root)));
        return bridge;
    }

    private static void Click(ElementInputBridge bridge, UIRoot root, UIElement element)
    {
        (float x, float y) = PointInside(element);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, previousDown: true));
    }

    private static void MovePointer(ElementInputBridge bridge, UIRoot root, UIElement element)
    {
        (float x, float y) = PointInside(element);
        bridge.Dispatch(root, PointerFrame(x, y));
    }

    private static (float X, float Y) PointInside(UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        return (bounds.X + MathF.Max(1, bounds.Width / 2), bounds.Y + MathF.Max(1, bounds.Height / 2));
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

    private static void Press(ElementInputBridge bridge, UIRoot root, InputKey key)
    {
        bridge.Dispatch(root, KeyboardFrame([], [key]));
    }

    private static void Release(ElementInputBridge bridge, UIRoot root, InputKey key)
    {
        bridge.Dispatch(root, KeyboardFrame([key], []));
    }

    private static InputFrame KeyboardFrame(IEnumerable<InputKey> previous, IEnumerable<InputKey> current)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previous),
            KeyboardSnapshot.FromDownKeys(current),
            []);
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

    private sealed record MenuEntry(string Label);
}
