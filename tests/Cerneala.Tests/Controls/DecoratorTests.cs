using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class DecoratorTests
{
    [Fact]
    public void ChildBecomesLogicalAndVisualChild()
    {
        Decorator decorator = new();
        UIElement child = new();

        decorator.Child = child;

        Assert.Same(decorator, child.LogicalParent);
        Assert.Same(decorator, child.VisualParent);
    }

    [Fact]
    public void ReplacingChildRemovesOldChild()
    {
        Decorator decorator = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        decorator.Child = oldChild;

        decorator.Child = newChild;

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(decorator, newChild.LogicalParent);
    }

    [Fact]
    public void RejectedReparentLeavesDecoratorWithoutPartialChild()
    {
        Decorator decorator = new();
        UIElement child = new();
        UIElement visualParent = new();
        visualParent.VisualChildren.Add(child);

        Assert.Throws<InvalidOperationException>(() => decorator.Child = child);

        Assert.Null(decorator.Child);
        Assert.Null(child.LogicalParent);
        Assert.Same(visualParent, child.VisualParent);
        Assert.DoesNotContain(child, decorator.LogicalChildren);
        Assert.DoesNotContain(child, decorator.VisualChildren);
    }

    [Fact]
    public void RejectedReparentDoesNotDetachExistingAttachedChild()
    {
        UIRoot root = new();
        Decorator decorator = new();
        LifecycleElement oldChild = new();
        UIElement newChild = new();
        UIElement visualParent = new();
        root.VisualChildren.Add(decorator);
        decorator.Child = oldChild;
        visualParent.VisualChildren.Add(newChild);
        int attachedCount = oldChild.AttachedCount;
        int detachedCount = oldChild.DetachedCount;

        Assert.Throws<InvalidOperationException>(() => decorator.Child = newChild);

        Assert.Same(oldChild, decorator.Child);
        Assert.Same(decorator, oldChild.LogicalParent);
        Assert.Same(decorator, oldChild.VisualParent);
        Assert.Equal(attachedCount, oldChild.AttachedCount);
        Assert.Equal(detachedCount, oldChild.DetachedCount);
    }

    [Fact]
    public void MeasuresAndArrangesChildInsideInsets()
    {
        Decorator decorator = new()
        {
            Padding = new Thickness(1),
            BorderThickness = new Thickness(2)
        };
        FixedElement child = new(new LayoutSize(10, 5));
        decorator.Child = child;

        LayoutSize desired = decorator.Measure(new MeasureContext(new LayoutSize(100, 100)));
        decorator.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), child.ArrangedBounds);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class LifecycleElement : UIElement
    {
        public int AttachedCount { get; private set; }

        public int DetachedCount { get; private set; }

        protected override void OnAttached()
        {
            AttachedCount++;
        }

        protected override void OnDetached()
        {
            DetachedCount++;
        }
    }
}
