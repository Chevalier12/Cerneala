using Cerneala.UI.Elements;

namespace Cerneala.Tests.UI.Elements;

public sealed class ElementLifecycleTests
{
    [Fact]
    public void AttachRunsParentBeforeVisualDescendants()
    {
        List<string> calls = [];
        TrackingElement parent = new("parent", calls);
        TrackingElement child = new("child", calls);
        parent.VisualChildren.Add(child);
        UIRoot root = new();

        root.VisualChildren.Add(parent);

        Assert.Equal(["attached:parent", "attached:child"], calls);
    }

    [Fact]
    public void DetachRunsVisualDescendantsBeforeParent()
    {
        List<string> calls = [];
        TrackingElement parent = new("parent", calls);
        TrackingElement child = new("child", calls);
        parent.VisualChildren.Add(child);
        UIRoot root = new();
        root.VisualChildren.Add(parent);
        calls.Clear();

        root.VisualChildren.Remove(parent);

        Assert.Equal(["detached:child", "detached:parent"], calls);
    }

    [Fact]
    public void AttachSubtreeRejectsElementAttachedToDifferentRoot()
    {
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        UIElement child = new();
        firstRoot.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => ElementLifecycle.AttachSubtree(secondRoot, child));
        Assert.Same(firstRoot, child.Root);
    }

    private sealed class TrackingElement(string name, List<string> calls) : UIElement
    {
        protected override void OnAttached()
        {
            calls.Add($"attached:{name}");
        }

        protected override void OnDetached()
        {
            calls.Add($"detached:{name}");
        }
    }
}
