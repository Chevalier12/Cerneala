using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.Input;

public sealed class ElementInputRouteBuilderTests
{
    [Fact]
    public void BuildRegistersAttachedElements()
    {
        UIRoot root = new();
        UIElement child = new();
        root.VisualChildren.Add(child);

        ElementInputRouteMap map = new ElementInputRouteBuilder().Build(root);

        Assert.True(map.Contains(root));
        Assert.True(map.Contains(child));
        Assert.Equal(2, map.Count);
        Assert.Same(root, map.ElementsInRouteOrder[0]);
        Assert.Same(child, map.ElementsInRouteOrder[1]);
    }

    [Fact]
    public void RouteMapKeepsDistinctEqualValueElementsDistinct()
    {
        EqualValueElement first = new(1);
        EqualValueElement second = new(1);
        ElementInputRouteMap map = new();

        map.Add(first, null);
        map.Add(second, first);

        Assert.True(map.Contains(first));
        Assert.True(map.Contains(second));
        Assert.Equal(2, map.Count);
        IReadOnlyList<UIElement> route = map.InputTree.GetRouteToRoot(second);
        Assert.Equal(2, route.Count);
        Assert.Same(second, route[0]);
        Assert.Same(first, route[1]);
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

        IReadOnlyList<UIElement> route = map.InputTree.GetRouteToRoot(child);
        Assert.Equal(3, route.Count);
        Assert.Same(child, route[0]);
        Assert.Same(parent, route[1]);
        Assert.Same(root, route[2]);
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

        Assert.False(map.Contains(disabled));
        Assert.False(map.Contains(invisible));
        Assert.True(map.Contains(includedGrandchild));
        IReadOnlyList<UIElement> route = map.InputTree.GetRouteToRoot(includedGrandchild);
        Assert.Equal(2, route.Count);
        Assert.Same(includedGrandchild, route[0]);
        Assert.Same(root, route[1]);
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
        Assert.True(map.Contains(child));

        RoutedEventRouter.Raise(map.InputTree, child, new RoutedEventArgs(routedEvent, child));

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
