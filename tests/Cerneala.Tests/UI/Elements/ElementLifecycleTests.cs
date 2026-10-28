using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
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
        Assert.False(leaf.IsAttached);
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

    // c0's Unloaded handler removes c0 from p while p is removed from the root. The walk still detaches c1, and
    // each child is unloaded and detached once (#886).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnloadedHandlerThatRemovesItsElementDoesNotSkipTheSiblings(bool handlerRemoves)
    {
        List<string> calls = [];
        UIRoot root = new();
        TrackingElement p = new("p", calls), c0 = new("c0", calls), c1 = new("c1", calls);
        p.VisualChildren.Add(c0);
        p.VisualChildren.Add(c1);
        root.VisualChildren.Add(p);
        int unloaded0 = 0, unloaded1 = 0;
        c0.Unloaded += (_, _) =>
        {
            unloaded0++;
            if (handlerRemoves)
            {
                p.VisualChildren.Remove(c0);
            }
        };
        c1.Unloaded += (_, _) => unloaded1++;
        calls.Clear();

        root.VisualChildren.Remove(p);

        Assert.Null(c0.Root);
        Assert.Null(c1.Root);
        Assert.Equal(1, unloaded0);
        Assert.Equal(1, unloaded1);
        Assert.Equal(1, calls.Count(call => call == "detached:c0"));
        Assert.Equal(1, calls.Count(call => call == "detached:c1"));
    }

    // c0's Loaded handler removes c0 from p while p is added to the root. The walk still attaches and loads c1 (#886).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadedHandlerThatRemovesItsElementDoesNotSkipTheSiblings(bool handlerRemoves)
    {
        List<string> calls = [];
        UIRoot root = new();
        TrackingElement p = new("p", calls), c0 = new("c0", calls), c1 = new("c1", calls);
        p.VisualChildren.Add(c0);
        p.VisualChildren.Add(c1);
        int loaded0 = 0, loaded1 = 0;
        c0.Loaded += (_, _) =>
        {
            loaded0++;
            if (handlerRemoves)
            {
                p.VisualChildren.Remove(c0);
            }
        };
        c1.Loaded += (_, _) => loaded1++;

        root.VisualChildren.Add(p);

        Assert.Same(root, c1.Root);
        Assert.Equal(1, loaded0);
        Assert.Equal(1, loaded1);
        Assert.True(c1.IsLoaded);
        Assert.Equal(1, calls.Count(call => call == "attached:c0"));
        Assert.Equal(1, calls.Count(call => call == "attached:c1"));
    }

    // c's Loaded handler removes c from the attached p while c is added to p. c and its child g end detached (#886).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadedHandlerThatRemovesItsElementDoesNotLeaveItsDescendantsAttached(bool handlerRemoves)
    {
        List<string> calls = [];
        UIRoot root = new();
        TrackingElement p = new("p", calls), c = new("c", calls), g = new("g", calls);
        root.VisualChildren.Add(p);
        c.VisualChildren.Add(g);
        c.Loaded += (_, _) =>
        {
            if (handlerRemoves)
            {
                p.VisualChildren.Remove(c);
            }
        };

        p.VisualChildren.Add(c);

        if (handlerRemoves)
        {
            Assert.Null(c.Root);
            Assert.Null(g.Root);
            Assert.False(g.IsLoaded);
            Assert.Empty(p.VisualChildren);
            Assert.DoesNotContain("attached:g", calls);
        }
        else
        {
            Assert.Same(root, g.Root);
        }
    }

    // The new Child's Loaded handler replaces it with another child. The replaced child's descendant ends detached
    // (#886, the Decorator path of the comment on PR #1117).
    [Fact]
    public void LoadedHandlerThatReplacesTheNewDecoratorChildDoesNotLeaveItsDescendantsAttached()
    {
        UIRoot root = new(200, 200);
        Border border = new();
        root.VisualChildren.Add(border);
        UIElement oldChild = new(), other = new(), inner = new();
        Border value = new() { Child = inner };
        border.Child = oldChild;
        root.ProcessFrame();
        bool done = false;
        value.Loaded += (_, _) =>
        {
            if (!done)
            {
                done = true;
                border.Child = other;
            }
        };

        border.Child = value;
        root.ProcessFrame();

        Assert.Same(other, border.Child);
        Assert.Equal([other], border.VisualChildren);
        Assert.Null(value.Root);
        Assert.Null(inner.Root);
        Assert.False(inner.IsLoaded);
        Assert.Same(root, other.Root);
    }

    // The new Content's Loaded handler replaces it with other content. The replaced content's descendant ends
    // detached (#886, the ContentControl path of the comment on PR #1116).
    [Fact]
    public void LoadedHandlerThatReplacesTheNewContentDoesNotLeaveItsDescendantsAttached()
    {
        UIRoot root = new(200, 200);
        ContentControl host = new();
        root.VisualChildren.Add(host);
        UIElement oldContent = new(), other = new(), inner = new();
        Border value = new() { Child = inner };
        host.Content = oldContent;
        root.ProcessFrame();
        bool done = false;
        value.Loaded += (_, _) =>
        {
            if (!done)
            {
                done = true;
                host.Content = other;
            }
        };

        host.Content = value;
        root.ProcessFrame();

        Assert.Same(other, host.Content);
        Assert.Null(value.Root);
        Assert.Null(inner.Root);
        Assert.False(inner.IsLoaded);
        Assert.Same(root, other.Root);
    }

    // Two children throw from OnDetached while their parent is removed. Every element is still detached and the
    // removal is still published, then the first child's exception reaches the caller.
    [Fact]
    public void ThrowingDetachCallbacksStillDetachTheWholeSubtreeBeforeTheFirstFailurePropagates()
    {
        List<string> calls = [];
        TrackingElement parent = new("parent", calls), sibling = new("sibling", calls);
        ThrowingDetachElement first = new("first"), second = new("second");
        parent.VisualChildren.Add(first);
        parent.VisualChildren.Add(sibling);
        parent.VisualChildren.Add(second);
        UIRoot root = new();
        root.VisualChildren.Add(parent);
        int removals = 0;
        root.VisualChildren.Changed += (_, change) =>
        {
            if (change.Kind == ElementTreeChangeKind.Removed) { removals++; }
        };
        calls.Clear();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(parent));

        Assert.Equal("detach failed: first", exception.Message);
        Assert.Equal(["detached:sibling", "detached:parent"], calls);
        foreach (UIElement element in new UIElement[] { first, sibling, second, parent })
        {
            Assert.Null(element.Root);
            Assert.False(element.IsAttached);
        }

        Assert.Empty(root.VisualChildren);
        Assert.Equal(1, removals);
    }

    // An Unloaded handler throws while an element with a lifecycle behavior is removed. The element's later
    // detach callbacks still run, then the handler's exception reaches the caller.
    [Fact]
    public void ThrowingUnloadedHandlerStillRunsTheElementsRemainingDetachCallbacks()
    {
        List<string> calls = [];
        TrackingElement element = new("element", calls);
        RecordingLifecycleBehavior behavior = new(calls);
        element.AddLifecycleBehavior(behavior);
        element.Unloaded += (_, _) => throw new InvalidOperationException("Unloaded handler failed");
        UIRoot root = new();
        root.VisualChildren.Add(element);
        calls.Clear();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(element));

        Assert.Equal("Unloaded handler failed", exception.Message);
        Assert.Equal(["behavior detached", "detached:element"], calls);
        Assert.Null(element.Root);
        Assert.False(element.IsAttached);
    }

    // An Unloaded handler throws, then a package behavior lifetime throws from Dispose while the button is
    // removed. The handler's exception reaches the caller, the lifetime is disposed once, and the button's
    // aspect state is cleared, so adding the button again creates a new lifetime.
    [Fact]
    public void ThrowingBehaviorLifetimeDoesNotReplaceTheEarlierUnloadedFailure()
    {
        int created = 0, disposed = 0;
        AspectBehavior behavior = new(typeof(Button), _ =>
        {
            created++;
            return new ThrowingLifetime(() =>
            {
                disposed++;
                throw new InvalidOperationException("lifetime dispose failed");
            });
        });
        UIRoot root = new();
        root.AspectRegistry.Register(AspectPackage.Create("P").Components(c => c.AddBehavior(behavior)));
        Button button = new();
        bool throwOnUnloaded = true;
        button.Unloaded += (_, _) =>
        {
            if (throwOnUnloaded) { throw new InvalidOperationException("Unloaded handler failed"); }
        };
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Equal(1, created);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(button));

        Assert.Equal("Unloaded handler failed", exception.Message);
        Assert.Equal(1, disposed);
        Assert.Null(button.Root);
        throwOnUnloaded = false;
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        Assert.Equal(2, created);
        Assert.Equal(1, disposed);
    }

    // A binding subscription throws from Dispose while its element is removed. The element is still detached
    // before the binding's exception reaches the caller, and adding it again attaches it normally.
    [Fact]
    public void ThrowingBindingDisposalStillLeavesTheElementDetached()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        element.Bindings.Add(new ThrowingLifetime(() => throw new InvalidOperationException("binding dispose failed")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(element));

        Assert.Equal("binding dispose failed", exception.Message);
        Assert.Null(element.Root);
        Assert.False(element.IsAttached);
        Assert.Equal(0, element.Bindings.Count);
        root.VisualChildren.Add(element);
        Assert.Same(root, element.Root);
        Assert.True(element.IsAttached);
    }

    // An Unloaded handler throws, then a binding subscription throws from Dispose while the element is removed.
    // The handler's exception, the first failure, reaches the caller and the element is detached.
    [Fact]
    public void ThrowingBindingDisposalDoesNotReplaceTheEarlierUnloadedFailure()
    {
        UIRoot root = new();
        UIElement element = new();
        element.Unloaded += (_, _) => throw new InvalidOperationException("Unloaded handler failed");
        root.VisualChildren.Add(element);
        element.Bindings.Add(new ThrowingLifetime(() => throw new InvalidOperationException("binding dispose failed")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => root.VisualChildren.Remove(element));

        Assert.Equal("Unloaded handler failed", exception.Message);
        Assert.Null(element.Root);
        Assert.False(element.IsAttached);
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

    private sealed class RecordingLifecycleBehavior(List<string> calls) : IElementLifecycleBehavior
    {
        public void Attach()
        {
            calls.Add("behavior attached");
        }

        public void Detach()
        {
            calls.Add("behavior detached");
        }
    }

    private sealed class ThrowingLifetime(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private sealed class ThrowingDetachElement(string name) : UIElement
    {
        protected override void OnDetached()
        {
            throw new InvalidOperationException($"detach failed: {name}");
        }
    }
}
