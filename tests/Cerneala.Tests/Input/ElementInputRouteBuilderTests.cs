using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class ElementInputRouteBuilderTests
{
    [Fact]
    public void BuildMapsAttachedElementsToIds()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        Assert.True(map.TryGetId(root, out UiElementId rootId));
        Assert.True(map.TryGetId(child, out UiElementId childId));
        Assert.True(map.TryGetElement(rootId, out UIElement? resolvedRoot));
        Assert.True(map.TryGetElement(childId, out UIElement? resolvedChild));
        Assert.Same(root, resolvedRoot);
        Assert.Same(child, resolvedChild);
    }

    [Fact]
    public void RouteMapMapsDistinctEqualValueElementsToDistinctIds()
    {
        EqualValueElement first = new(1);
        EqualValueElement second = new(1);
        ElementInputRouteMap map = new();
        UiElementId firstId = new("first");
        UiElementId secondId = new("second");

        map.Add(first, firstId, null);
        map.Add(second, secondId, firstId);

        Assert.True(map.TryGetId(first, out UiElementId resolvedFirstId));
        Assert.True(map.TryGetId(second, out UiElementId resolvedSecondId));
        Assert.Equal(firstId, resolvedFirstId);
        Assert.Equal(secondId, resolvedSecondId);
        Assert.True(map.TryGetElement(firstId, out UIElement? resolvedFirst));
        Assert.True(map.TryGetElement(secondId, out UIElement? resolvedSecond));
        Assert.Same(first, resolvedFirst);
        Assert.Same(second, resolvedSecond);
    }

    [Fact]
    public void BuildUsesVisualAncestryForRouteOrder()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);

        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.TryGetId(child, out UiElementId childId));
        Assert.True(map.TryGetId(parent, out UiElementId parentId));
        Assert.True(map.TryGetId(root, out UiElementId rootId));

        Assert.Equal([childId, parentId, rootId], map.InputTree.GetRouteToRoot(childId));
    }

    [Fact]
    public void BuildExcludesDisabledOrInvisibleElements()
    {
        UIRoot root = new();
        UIElement disabled = new() { IsEnabled = false };
        UIElement invisible = new() { IsVisible = false };
        UIElement includedGrandchild = new();
        disabled.VisualChildren.Add(includedGrandchild);
        root.VisualChildren.Add(disabled);
        root.VisualChildren.Add(invisible);

        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        Assert.False(map.TryGetId(disabled, out _));
        Assert.False(map.TryGetId(invisible, out _));
        Assert.True(map.TryGetId(includedGrandchild, out UiElementId grandchildId));
        Assert.True(map.TryGetId(root, out UiElementId rootId));
        Assert.Equal([grandchildId, rootId], map.InputTree.GetRouteToRoot(grandchildId));
    }

    [Fact]
    public void BuildExportsElementHandlersToInputTree()
    {
        RoutedEvent routedEvent = RoutedEventRegistry.Register(
            "RetainedClick",
            typeof(ElementInputRouteBuilderTests),
            RoutingStrategy.Bubble,
            typeof(RoutedEventArgs));
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);
        bool handled = false;
        child.Handlers.AddHandler(routedEvent, (_, _) => handled = true);

        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);
        Assert.True(map.TryGetId(child, out UiElementId childId));

        RoutedEventRouter.Raise(map.InputTree, childId, new RoutedEventArgs(routedEvent, childId));

        Assert.True(handled);
    }

    private sealed class EqualValueElement(int value) : UIElement
    {
        private readonly int value = value;

        public override bool Equals(object? obj)
        {
            return obj is EqualValueElement other && other.value == value;
        }

        public override int GetHashCode()
        {
            return value;
        }
    }
}
