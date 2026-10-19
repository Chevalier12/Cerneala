using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ContentControlTests
{
    [Fact]
    public void UiElementContentBecomesLogicalAndVisualChild()
    {
        ContentControl control = new();
        UIElement child = new();

        control.Content = child;

        Assert.Same(control, child.LogicalParent);
        Assert.Same(control, child.VisualParent);
        Assert.Contains(child, control.LogicalChildren);
        Assert.Contains(child, control.VisualChildren);
    }

    [Fact]
    public void ReplacingContentRemovesOldElement()
    {
        ContentControl control = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        control.Content = oldChild;

        control.Content = newChild;

        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(control, newChild.LogicalParent);
        Assert.Same(control, newChild.VisualParent);
    }

    [Fact]
    public void ReplacingEqualElementContentUpdatesRetainedOwnership()
    {
        ContentControl control = new();
        EqualElement oldChild = new(1);
        EqualElement newChild = new(1);
        control.Content = oldChild;
        int layoutVersion = control.LayoutVersion;
        int renderVersion = control.RenderVersion;

        control.Content = newChild;

        Assert.Same(newChild, control.Content);
        Assert.True(control.LayoutVersion > layoutVersion);
        Assert.True(control.RenderVersion > renderVersion);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(control, newChild.LogicalParent);
        Assert.Same(control, newChild.VisualParent);
    }

    [Fact]
    public void TemplateContentPresenterCanHostExistingElementContent()
    {
        ContentControl control = new();
        UIElement child = new();
        ContentPresenter? presenter = null;
        control.Content = child;

        control.ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
        {
            presenter = new ContentPresenter { Content = context.Owner.Content };
            return presenter;
        });

        Assert.Same(child, presenter!.PresentedChild);
        Assert.Same(presenter, child.LogicalParent);
        Assert.Same(presenter, child.VisualParent);
        Assert.DoesNotContain(child, control.LogicalChildren);
        Assert.DoesNotContain(child, control.VisualChildren);
        Assert.Contains(presenter, control.LogicalChildren);
        Assert.Contains(presenter, control.VisualChildren);
    }

    [Fact]
    public void RejectedReparentLeavesExistingContentOwned()
    {
        ContentControl control = new();
        UIElement oldChild = new();
        UIElement newChild = new();
        UIElement visualParent = new();
        control.Content = oldChild;
        visualParent.VisualChildren.Add(newChild);

        Assert.Throws<InvalidOperationException>(() => control.Content = newChild);

        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Null(newChild.LogicalParent);
        Assert.Same(visualParent, newChild.VisualParent);
        Assert.DoesNotContain(newChild, control.LogicalChildren);
        Assert.DoesNotContain(newChild, control.VisualChildren);
    }

    [Fact]
    public void RejectedReparentDoesNotDetachExistingAttachedContent()
    {
        UIRoot root = new();
        ContentControl control = new();
        LifecycleElement oldChild = new();
        UIElement newChild = new();
        UIElement visualParent = new();
        root.VisualChildren.Add(control);
        control.Content = oldChild;
        visualParent.VisualChildren.Add(newChild);
        int attachedCount = oldChild.AttachedCount;
        int detachedCount = oldChild.DetachedCount;

        Assert.Throws<InvalidOperationException>(() => control.Content = newChild);

        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Equal(attachedCount, oldChild.AttachedCount);
        Assert.Equal(detachedCount, oldChild.DetachedCount);
    }

    [Fact]
    public void SetValueRejectsTemplatedElementContentThatHasAnotherParentLikeTheContentSetter()
    {
        ContentControl control = new()
        {
            ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
                new ContentPresenter { Content = context.Owner.Content })
        };
        UIElement oldChild = new();
        UIElement parented = new();
        UIElement parent = new();
        control.Content = oldChild;
        parent.LogicalChildren.Add(parented);

        Assert.Throws<InvalidOperationException>(() => control.Content = parented);
        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => control.SetValue(ContentControl.ContentProperty, (object?)parented));

        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Same(oldChild, control.Content);
        Assert.Same(parent, parented.LogicalParent);

        UIElement loose = new();
        control.SetValue(ContentControl.ContentProperty, (object?)loose);
        Assert.Same(loose, control.Content);
    }

    [Fact]
    public void ClearValueRejectsTemplatedLowerSourceElementContentThatGotAnotherParent()
    {
        UIRoot root = new(300, 200);
        ContentControl control = new()
        {
            ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
                new ContentPresenter { Content = context.Owner.Content }),
            Content = "text"
        };
        Border holder = new();
        Border revealed = new() { Width = 10, Height = 10 };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        control.SetValue(ContentControl.ContentProperty, (object?)revealed, UiPropertyValueSource.AspectBase);
        holder.Child = revealed;
        root.VisualChildren.Add(holder);

        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => control.ClearValue(ContentControl.ContentProperty));

        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Equal("text", control.Content);
        Assert.Equal(UiPropertyValueSource.Local, control.GetValueSource(ContentControl.ContentProperty));
        Assert.Same(holder, revealed.LogicalParent);
        root.ProcessFrame();
        Assert.Same(holder, revealed.VisualParent);
    }

    [Fact]
    public void SetValueStoresShadowedTemplatedElementContentThatHasAnotherParentWithoutChangingContent()
    {
        ContentControl control = new()
        {
            ComponentTemplate = new ComponentTemplate<ContentControl>("test", context =>
                new ContentPresenter { Content = context.Owner.Content }),
            Content = "text"
        };
        UIElement parented = new();
        UIElement parent = new();
        parent.LogicalChildren.Add(parented);
        parent.VisualChildren.Add(parented);

        control.SetValue(ContentControl.ContentProperty, (object?)parented, UiPropertyValueSource.MarkupConditional);

        Assert.Equal("text", control.Content);
        Assert.Equal(UiPropertyValueSource.Local, control.GetValueSource(ContentControl.ContentProperty));
        Assert.Same(parented, control.GetSourceValue(ContentControl.ContentProperty, UiPropertyValueSource.MarkupConditional));
        Assert.Same(parent, parented.LogicalParent);
        Assert.Same(parent, parented.VisualParent);
    }

    [Fact]
    public void SetValueHostsUntemplatedElementContentLikeTheContentSetter()
    {
        ContentControl control = new();
        UIElement first = new();
        UIElement second = new();

        control.SetValue(ContentControl.ContentProperty, (object?)first);

        Assert.Same(control, first.LogicalParent);
        Assert.Same(control, first.VisualParent);

        control.SetValue(ContentControl.ContentProperty, (object?)second);

        Assert.Null(first.LogicalParent);
        Assert.Null(first.VisualParent);
        Assert.Same(control, second.LogicalParent);
        Assert.Same(control, second.VisualParent);

        control.ClearValue(ContentControl.ContentProperty);

        Assert.Null(control.Content);
        Assert.Null(second.LogicalParent);
        Assert.Null(second.VisualParent);
        Assert.Empty(control.LogicalChildren);
        Assert.Empty(control.VisualChildren);
    }

    [Fact]
    public void ClearingAValueSourceHostsTheElementContentOfTheNextSource()
    {
        ContentControl control = new();
        UIElement conditional = new();
        UIElement local = new();
        control.SetValue(ContentControl.ContentProperty, (object?)conditional, UiPropertyValueSource.MarkupConditional);

        control.SetValue(ContentControl.ContentProperty, (object?)local);

        Assert.Same(local, control.Content);
        Assert.Same(control, local.VisualParent);
        Assert.Null(conditional.LogicalParent);
        Assert.Null(conditional.VisualParent);

        control.ClearValue(ContentControl.ContentProperty);

        Assert.Same(conditional, control.Content);
        Assert.Same(control, conditional.LogicalParent);
        Assert.Same(control, conditional.VisualParent);
        Assert.Null(local.LogicalParent);
        Assert.Null(local.VisualParent);
    }

    [Fact]
    public void SetValueRejectsUntemplatedElementContentThatHasAnotherParentLikeTheContentSetter()
    {
        ContentControl control = new();
        UIElement oldChild = new();
        UIElement parented = new();
        UIElement parent = new();
        control.Content = oldChild;
        parent.LogicalChildren.Add(parented);

        Assert.Throws<InvalidOperationException>(() => control.Content = parented);
        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(
            () => control.SetValue(ContentControl.ContentProperty, (object?)parented));

        Assert.Equal("Element must be removed from its current parent before reparenting.", rejected.Message);
        Assert.Same(oldChild, control.Content);
        Assert.Same(control, oldChild.LogicalParent);
        Assert.Same(control, oldChild.VisualParent);
        Assert.Same(parent, parented.LogicalParent);
        Assert.DoesNotContain(parented, control.LogicalChildren);
    }

    [Fact]
    public void MeasuresAndArrangesContentInsideInsets()
    {
        ContentControl control = new()
        {
            Padding = new Thickness(2),
            BorderThickness = new Thickness(1)
        };
        FixedElement child = new(new LayoutSize(10, 5));
        control.Content = child;

        LayoutSize desired = control.Measure(new MeasureContext(new LayoutSize(100, 100)));
        control.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

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

    private sealed class EqualElement(int id) : UIElement
    {
        private readonly int id = id;

        public override bool Equals(object? obj)
        {
            return obj is EqualElement other && other.id == id;
        }

        public override int GetHashCode()
        {
            return id;
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
