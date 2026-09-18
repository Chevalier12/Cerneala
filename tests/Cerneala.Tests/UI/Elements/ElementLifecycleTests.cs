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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AttachmentIncludesMixedRoleDescendants(bool logicalFirst)
    {
        List<string> calls = [];
        TrackingElement parent = new("parent", calls), middle = new("middle", calls), leaf = new("leaf", calls);
        Connect(parent, middle, leaf, logicalFirst);
        UIRoot root = new();

        root.VisualChildren.Add(parent);

        Assert.Same(root, leaf.Root);
        Assert.Equal(["attached:parent", "attached:middle", "attached:leaf"], calls);
        root.VisualChildren.Remove(parent);
        Assert.Null(leaf.Root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachmentIncludesMixedRoleDescendants(bool logicalFirst)
    {
        List<string> calls = [];
        TrackingElement parent = new("parent", calls), middle = new("middle", calls), leaf = new("leaf", calls);
        Connect(parent, middle, leaf, logicalFirst);
        UIRoot root = new();
        root.VisualChildren.Add(parent);
        ElementLifecycle.AttachSubtree(root, leaf);
        calls.Clear();

        root.VisualChildren.Remove(parent);

        Assert.Null(leaf.Root);
        Assert.Null(leaf.ElementId);
        Assert.Equal(["detached:leaf", "detached:middle", "detached:parent"], calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedRoleForeignRootIsRejectedBeforeAnyAttachment(bool logicalFirst)
    {
        UIElement parent = new(), middle = new(), leaf = new();
        Connect(parent, middle, leaf, logicalFirst);
        UIRoot root = new(), foreign = new();
        ElementLifecycle.AttachSubtree(foreign, leaf);

        Assert.Throws<InvalidOperationException>(() => ElementLifecycle.AttachSubtree(root, parent));

        Assert.Null(parent.Root);
        Assert.Null(middle.Root);
        Assert.Same(foreign, leaf.Root);
        ElementLifecycle.DetachSubtree(foreign, leaf);
    }

    [Fact]
    public void SharedRoleDescendantsReceiveEachLifecycleCallbackOnce()
    {
        List<string> calls = [];
        TrackingElement parent = new("parent", calls), child = new("child", calls);
        parent.LogicalChildren.Add(child);
        parent.VisualChildren.Add(child);
        UIRoot root = new();
        root.VisualChildren.Add(parent);
        root.VisualChildren.Remove(parent);
        Assert.Equal(["attached:parent", "attached:child", "detached:child", "detached:parent"], calls);
    }

    private static void Connect(UIElement parent, UIElement middle, UIElement leaf, bool logicalFirst)
    {
        (logicalFirst ? parent.LogicalChildren : parent.VisualChildren).Add(middle);
        (logicalFirst ? middle.VisualChildren : middle.LogicalChildren).Add(leaf);
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
