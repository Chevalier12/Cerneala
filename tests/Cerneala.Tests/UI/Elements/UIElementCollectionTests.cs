using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIElementCollectionTests
{
    [Fact]
    public void AddSetsMatchingParentAndRaisesChange()
    {
        UIElement parent = new();
        UIElement child = new();
        ElementTreeChange? change = null;
        parent.VisualChildren.Changed += (_, args) => change = args;

        parent.VisualChildren.Add(child);

        Assert.Same(parent, child.VisualParent);
        Assert.Single(parent.VisualChildren);
        Assert.NotNull(change);
        Assert.Same(parent, change.Parent);
        Assert.Same(child, change.Child);
        Assert.Equal(ElementChildRole.Visual, change.Role);
        Assert.Equal(ElementTreeChangeKind.Added, change.Kind);
    }

    [Fact]
    public void RemoveClearsMatchingParentAndRaisesChange()
    {
        UIElement parent = new();
        UIElement child = new();
        parent.VisualChildren.Add(child);
        ElementTreeChange? change = null;
        parent.VisualChildren.Changed += (_, args) => change = args;

        bool removed = parent.VisualChildren.Remove(child);

        Assert.True(removed);
        Assert.Null(child.VisualParent);
        Assert.Empty(parent.VisualChildren);
        Assert.NotNull(change);
        Assert.Equal(ElementTreeChangeKind.Removed, change.Kind);
    }

    [Fact]
    public void RemovingAttachedVisualChildInvalidatesOwnerLayoutRenderAndHitTesting()
    {
        UIRoot root = new();
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();

        parent.VisualChildren.Remove(child);

        Assert.Contains(parent, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(parent, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(parent, root.RenderQueue.Snapshot());
        Assert.Contains(parent, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void RemovingDetachedVisualChildInvalidatesOwnerLayoutCache()
    {
        UIElement parent = new MeasuringParent();
        UIElement child = new FixedElement(new LayoutSize(10, 7));
        parent.VisualChildren.Add(child);
        MeasureContext context = new(new LayoutSize(100, 100));
        LayoutSize first = parent.Measure(context);

        parent.VisualChildren.Remove(child);
        LayoutSize second = parent.Measure(context);

        Assert.Equal(new LayoutSize(10, 7), first);
        Assert.Equal(LayoutSize.Zero, second);
        Assert.True(parent.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(parent.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void DuplicateAddIsRejected()
    {
        UIElement parent = new();
        UIElement child = new();
        parent.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => parent.VisualChildren.Add(child));
    }

    [Fact]
    public void DistinctEqualValueChildrenCanBeAddedToSameCollection()
    {
        UIElement parent = new();
        EqualValueElement first = new(1);
        EqualValueElement second = new(1);

        parent.VisualChildren.Add(first);
        parent.VisualChildren.Add(second);

        Assert.Equal(2, parent.VisualChildren.Count);
        Assert.Same(first, parent.VisualChildren[0]);
        Assert.Same(second, parent.VisualChildren[1]);
        Assert.Same(parent, first.VisualParent);
        Assert.Same(parent, second.VisualParent);
    }

    [Fact]
    public void RemoveUsesReferenceIdentity()
    {
        UIElement parent = new();
        EqualValueElement actualChild = new(1);
        EqualValueElement equalNonChild = new(1);
        parent.VisualChildren.Add(actualChild);

        bool removed = parent.VisualChildren.Remove(equalNonChild);

        Assert.False(removed);
        Assert.Single(parent.VisualChildren);
        Assert.Same(actualChild, parent.VisualChildren[0]);
        Assert.Same(parent, actualChild.VisualParent);
        Assert.Null(equalNonChild.VisualParent);
    }

    [Fact]
    public void ElementCannotBeChildOfItself()
    {
        UIElement element = new();

        Assert.Throws<InvalidOperationException>(() => element.VisualChildren.Add(element));
    }

    [Fact]
    public void AncestorCannotBeAddedAsChild()
    {
        UIElement root = new();
        UIElement child = new();
        UIElement grandchild = new();
        root.VisualChildren.Add(child);
        child.VisualChildren.Add(grandchild);

        Assert.Throws<InvalidOperationException>(() => grandchild.VisualChildren.Add(root));
        Assert.Null(root.VisualParent);
    }

    [Fact]
    public void AttachedElementCannotBeAddedUnderDifferentRoot()
    {
        UIRoot firstRoot = new();
        UIRoot secondRoot = new();
        UIElement child = new();
        firstRoot.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => secondRoot.LogicalChildren.Add(child));
        Assert.Null(child.LogicalParent);
        Assert.Same(firstRoot, child.Root);
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

    private sealed class MeasuringParent : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            float width = 0;
            float height = 0;
            foreach (UIElement child in VisualChildren)
            {
                LayoutSize childSize = child.Measure(context);
                width = Math.Max(width, childSize.Width);
                height += childSize.Height;
            }

            return new LayoutSize(width, height);
        }
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
