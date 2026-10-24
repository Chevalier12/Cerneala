using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class MenuSemanticsTests
{
    [Fact]
    public void FactorySelectsDedicatedPeersForEveryMenuType()
    {
        Menu menu = new();
        MenuBar menuBar = new();
        MenuItem menuItem = new();

        MenuAutomationPeer menuPeer = Assert.IsType<MenuAutomationPeer>(AutomationPeer.Create(menu));
        MenuAutomationPeer menuBarPeer = Assert.IsType<MenuAutomationPeer>(AutomationPeer.Create(menuBar));
        MenuItemAutomationPeer itemPeer = Assert.IsType<MenuItemAutomationPeer>(AutomationPeer.Create(menuItem));

        Assert.Equal(SemanticsRole.Menu, menuPeer.Role);
        Assert.Equal(SemanticsRole.MenuBar, menuBarPeer.Role);
        Assert.Equal(SemanticsRole.MenuItem, itemPeer.Role);
    }

    [Fact]
    public void MenuTreeReportsHeaderCountFocusAndExpandedStateAcrossOpenClose()
    {
        MenuItem child = new() { Header = "Open" };
        MenuItem parent = new() { Header = "File" };
        parent.Items.Add(child);
        Menu menu = new();
        menu.Items.Add(parent);
        UIRoot root = new(240, 160);
        root.VisualChildren.Add(menu);
        root.ProcessFrame();
        FocusManager focus = new();
        Assert.True(focus.Focus(parent, root.InputCache.EnsureCurrent(root)));

        parent.IsSubmenuOpen = true;
        root.ProcessFrame();
        SemanticsTree openTree = root.GetSemanticsTree();
        SemanticsNode openMenu = Find(openTree.Root, menu);
        SemanticsNode openParent = Find(openTree.Root, parent);

        Assert.Equal(SemanticsRole.Menu, openMenu.Role);
        Assert.Equal(1, openMenu.GetProperty<int>(SemanticsProperty.ItemCount));
        Assert.Equal(SemanticsRole.MenuItem, openParent.Role);
        Assert.Equal("File", openParent.Name);
        Assert.Equal(1, openParent.GetProperty<int>(SemanticsProperty.ItemCount));
        Assert.True(openParent.GetProperty<bool>(SemanticsProperty.IsEnabled));
        Assert.True(openParent.GetProperty<bool>(SemanticsProperty.IsFocused));
        Assert.True(openParent.GetProperty<bool>(SemanticsProperty.IsExpanded));
        Assert.Equal("Open", Find(openTree.Root, child).Name);

        parent.IsSubmenuOpen = false;
        root.ProcessFrame();
        SemanticsTree closedTree = root.GetSemanticsTree();
        SemanticsNode closedParent = Find(closedTree.Root, parent);

        Assert.NotSame(openTree, closedTree);
        Assert.False(closedParent.GetProperty<bool>(SemanticsProperty.IsExpanded));
        Assert.Null(FindOrDefault(closedTree.Root, child));

        parent.IsEnabled = false;
        root.ProcessFrame();
        SemanticsNode disabledParent = Find(root.GetSemanticsTree().Root, parent);

        Assert.False(disabledParent.GetProperty<bool>(SemanticsProperty.IsEnabled));
    }

    private static SemanticsNode Find(SemanticsNode node, UIElement? element)
    {
        return FindOrDefault(node, element) ?? throw new Xunit.Sdk.XunitException($"No semantics node exists for the {element?.GetType().Name}.");
    }

    private static SemanticsNode? FindOrDefault(SemanticsNode node, UIElement? element)
    {
        if (ReferenceEquals(node.Element, element))
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? match = FindOrDefault(child, element);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
