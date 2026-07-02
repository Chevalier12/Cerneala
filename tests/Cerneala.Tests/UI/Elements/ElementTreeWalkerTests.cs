using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Elements;

public sealed class ElementTreeWalkerTests
{
    [Fact]
    public void WalkersValidateNullArgumentsImmediately()
    {
        Assert.Throws<ArgumentNullException>(() => ElementTreeWalker.PreOrder(null!));
        Assert.Throws<ArgumentNullException>(() => ElementTreeWalker.PostOrder(null!));
        Assert.Throws<ArgumentNullException>(() => ElementTreeWalker.Ancestors(null!));
        Assert.Throws<ArgumentNullException>(() => ElementTreeWalker.Descendants(null!));
    }

    [Fact]
    public void PreOrderVisitsParentBeforeChildren()
    {
        (UIElement root, UIElement first, UIElement second, UIElement grandchild) = CreateTree();

        UIElement[] walked = ElementTreeWalker.PreOrder(root).ToArray();

        Assert.Equal([root, first, grandchild, second], walked);
    }

    [Fact]
    public void PostOrderVisitsChildrenBeforeParent()
    {
        (UIElement root, UIElement first, UIElement second, UIElement grandchild) = CreateTree();

        UIElement[] walked = ElementTreeWalker.PostOrder(root).ToArray();

        Assert.Equal([grandchild, first, second, root], walked);
    }

    [Fact]
    public void AncestorsWalkNearestParentToRoot()
    {
        (UIElement root, UIElement first, _, UIElement grandchild) = CreateTree();

        UIElement[] ancestors = ElementTreeWalker.Ancestors(grandchild).ToArray();

        Assert.Equal([first, root], ancestors);
    }

    [Fact]
    public void DescendantsSkipRoot()
    {
        (UIElement root, UIElement first, UIElement second, UIElement grandchild) = CreateTree();

        UIElement[] descendants = ElementTreeWalker.Descendants(root).ToArray();

        Assert.Equal([first, grandchild, second], descendants);
    }

    private static (UIElement Root, UIElement First, UIElement Second, UIElement Grandchild) CreateTree()
    {
        UIElement root = new();
        UIElement first = new();
        UIElement second = new();
        UIElement grandchild = new();

        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        first.VisualChildren.Add(grandchild);

        return (root, first, second, grandchild);
    }
}
