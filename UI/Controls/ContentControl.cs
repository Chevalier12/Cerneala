using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public class ContentControl : Control
{
    internal static readonly IEqualityComparer<object?> ContentEqualityComparer = new ContentValueEqualityComparer();

    // The UIElement Content this control added as its direct child. The element leaves because this control added it,
    // not because HostsContentDirectly answers true again: a subclass's answer can change, for example when a template
    // predicate reads state that changed.
    private UIElement? hostedContentElement;

    // Set when OnPropertyChanged could not host the new Content element, so NotifyPropertyChange restores the old value.
    private bool contentHostingFailed;

    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(ContentControl),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            ContentEqualityComparer));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set
        {
            object? oldContent = Content;
            try
            {
                SetValue(ContentProperty, value);
            }
            catch
            {
                // Hosting failed after the new value was stored: storing the old value again hosts the old content.
                if (!ContentEqualityComparer.Equals(Content, oldContent))
                {
                    SetValue(ContentProperty, oldContent);
                }

                throw;
            }
        }
    }

    public ResourceId<FontResource>? FontResourceId { get; set; }

    public IResourceProvider? ResourceProvider { get; set; }

    protected UIElement? ContentElement => Content as UIElement;

    // True when this control is the direct parent of the given content when that content is a UIElement.
    internal virtual bool HostsContentDirectly(object? content) => ComponentTemplate is null && ComponentTemplateKey is null;

    // Every write of Content that becomes the effective Content, the setter and SetValue with any value source, and a
    // ClearValue that makes a lower source's element effective, checks the element here, before the store changes,
    // whether this control hosts it (OnPropertyChanged) or another owner takes it later (a ContentPresenter, or the
    // TabItem that holds it). A shadowed write does not change Content, so it is not checked, as in WPF, where the
    // ownership work runs in the property-changed callback of ContentProperty.
    internal override void ValidateEffectiveValueChange(UiProperty property, object? value)
    {
        base.ValidateEffectiveValueChange(property, value);
        if (ReferenceEquals(property, ContentProperty) &&
            value is UIElement element &&
            !ReferenceEquals(element, Content))
        {
            ValidateCanOwnChild(this, element);
        }
    }

    // A change of the effective Content whose new element could not be hosted, for example because its attach threw,
    // restores the previous value at the value source that changed, which hosts the previous element again, and then
    // rethrows. So a failed replacement restores the previous content ownership whatever wrote it: the setter, or
    // SetValue and ClearValue with any value source.
    internal override void NotifyPropertyChange(in UiPropertyChange change)
    {
        if (!ReferenceEquals(change.Args.Property, ContentProperty))
        {
            base.NotifyPropertyChange(change);
            return;
        }

        contentHostingFailed = false;
        try
        {
            base.NotifyPropertyChange(change);
        }
        catch when (contentHostingFailed && change.MutatingSource != UiPropertyValueSource.Default)
        {
            contentHostingFailed = false;
            RestoreContentSource(change);
            throw;
        }
    }

    // A write at the source became the effective value: the source held the old value when it was the old effective
    // source, and held nothing otherwise. A clear of the source revealed a lower value: the source held the old value.
    private void RestoreContentSource(in UiPropertyChange change)
    {
        UiPropertyValueSource source = change.MutatingSource;
        if (change.Args.ValueSource == source && change.OldEffectiveSource != source)
        {
            ClearValueUntyped(ContentProperty, source);
        }
        else
        {
            SetValueUntyped(ContentProperty, change.OldSourceValue, source);
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        Thickness insets = Insets;
        LayoutSize available = Deflate(context.AvailableSize, insets);
        LayoutSize contentSize = ContentElement?.Measure(new MeasureContext(available, context.Rounding)) ?? LayoutSize.Zero;
        return Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        Thickness insets = Insets;
        LayoutRect inner = Deflate(context.FinalRect, insets);
        ContentElement?.Arrange(new ArrangeContext(inner, context.Rounding));
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        // The effective Content changed through any path (the setter, SetValue or ClearValue with any value source),
        // so the hosted element follows it, as WPF's ContentControl.OnContentChanged moves the logical child.
        if (ReferenceEquals(args.Property, ContentProperty))
        {
            RemoveContentElement(hostedContentElement);
            if (HostsContentDirectly(args.NewValue))
            {
                try
                {
                    AddContentElement(args.NewValue);
                }
                catch
                {
                    contentHostingFailed = true;
                    throw;
                }
            }

            base.OnPropertyChanged(args);
            return;
        }

        if (!ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            !ReferenceEquals(args.Property, ComponentTemplateKeyProperty))
        {
            base.OnPropertyChanged(args);
            return;
        }

        ReleaseContentElementFromOwnedSubtree();
        base.OnPropertyChanged(args);
        if (HostsContentDirectly(Content))
        {
            AddContentElement(Content);
        }
    }

    // A subclass calls this after its own state changed what HostsContentDirectly returns for Content. Content that
    // this control starts to host can still be inside an element that its previous presentation built for it, for
    // example a template's element in a ContentPresenter child, so it leaves that subtree first.
    internal void UpdateContentHosting()
    {
        UIElement? wanted = HostsContentDirectly(Content) ? Content as UIElement : null;
        if (ReferenceEquals(hostedContentElement, wanted))
        {
            return;
        }

        RemoveContentElement(hostedContentElement);
        if (wanted is not null)
        {
            DetachChildFromOwnedSubtree(this, wanted);
        }

        AddContentElement(wanted);
    }

    internal static LayoutSize Deflate(LayoutSize size, Thickness thickness)
    {
        float width = size.IsWidthUnconstrained ? float.PositiveInfinity : MathF.Max(0, size.Width - thickness.Horizontal);
        float height = size.IsHeightUnconstrained ? float.PositiveInfinity : MathF.Max(0, size.Height - thickness.Vertical);
        return new LayoutSize(width, height);
    }

    internal static LayoutSize Inflate(LayoutSize size, Thickness thickness)
    {
        float width = size.IsWidthUnconstrained ? float.PositiveInfinity : size.Width + thickness.Horizontal;
        float height = size.IsHeightUnconstrained ? float.PositiveInfinity : size.Height + thickness.Vertical;
        return new LayoutSize(width, height);
    }

    internal static LayoutRect Deflate(LayoutRect rect, Thickness thickness)
    {
        return new LayoutRect(
            rect.X + thickness.Left,
            rect.Y + thickness.Top,
            MathF.Max(0, rect.Width - thickness.Horizontal),
            MathF.Max(0, rect.Height - thickness.Vertical));
    }

    internal static void ValidateCanOwnChild(UIElement owner, UIElement? child)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (child is null)
        {
            return;
        }

        if (ReferenceEquals(owner, child))
        {
            throw new InvalidOperationException("An element cannot be added as a child of itself.");
        }

        if (IsAncestor(owner, child, ElementChildRole.Logical) ||
            IsAncestor(owner, child, ElementChildRole.Visual))
        {
            throw new InvalidOperationException("An ancestor cannot be added as a child.");
        }

        child.ReleaseFromParentForNewOwner();
        if (child.LogicalParent is not null || child.VisualParent is not null)
        {
            throw new InvalidOperationException("Element must be removed from its current parent before reparenting.");
        }

        if (owner.Root is not null && child.Root is not null && !ReferenceEquals(owner.Root, child.Root))
        {
            throw new InvalidOperationException("Element cannot be added under a different root.");
        }
    }

    internal static void DetachChildFromOwnedSubtree(UIElement owner, UIElement child)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(child);

        // A Decorator also names its child in Child. Releasing the element through Child keeps that property in step
        // with the Decorator's children, so the Decorator can take the element again, for example when a template
        // reuses it as its container.
        if (child.VisualParent is Decorator decorator &&
            ReferenceEquals(decorator.Child, child) &&
            IsOwnedBy(decorator, owner, ElementChildRole.Visual))
        {
            decorator.Child = null;
        }

        // A ContentControl that hosts its content directly also names that element in Content. Releasing the element
        // through Content keeps the property in step with the control's children, so the control can take the
        // element again, for example when a template reuses it as its container.
        if (child.VisualParent is ContentControl contentControl &&
            !ReferenceEquals(contentControl, owner) &&
            contentControl.HostsContentDirectly(child) &&
            ReferenceEquals(contentControl.Content, child) &&
            IsOwnedBy(contentControl, owner, ElementChildRole.Visual))
        {
            contentControl.Content = null;
        }

        // A ContentPresenter that presents its content directly also names that element in Content and PresentedChild.
        // Releasing the element through Content keeps both in step with the presenter's children, so the presenter can
        // present the element again, for example when a template reuses it as its container.
        if (child.VisualParent is ContentPresenter presenter &&
            !ReferenceEquals(presenter, owner) &&
            ReferenceEquals(presenter.PresentedChild, child) &&
            ReferenceEquals(presenter.Content, child) &&
            IsOwnedBy(presenter, owner, ElementChildRole.Visual))
        {
            presenter.Content = null;
        }

        // A TabItem that hosts its header directly also names that element in Header. Releasing the element through
        // Header keeps the property in step with the tab's children, so the tab can take the element again, for
        // example when a template reuses it as its container.
        if (child.VisualParent is TabItem tabItem &&
            !ReferenceEquals(tabItem, owner) &&
            tabItem.HostsHeaderDirectly(child) &&
            IsOwnedBy(tabItem, owner, ElementChildRole.Visual))
        {
            tabItem.Header = null;
        }

        UIElement? visualParent = child.VisualParent;
        if (IsOwnedBy(visualParent, owner, ElementChildRole.Visual))
        {
            visualParent!.VisualChildren.Remove(child);
        }

        UIElement? logicalParent = child.LogicalParent;
        if (IsOwnedBy(logicalParent, owner, ElementChildRole.Logical))
        {
            logicalParent!.LogicalChildren.Remove(child);
        }
    }

    private void AddContentElement(object? content)
    {
        if (content is not UIElement element)
        {
            return;
        }

        // An attach that throws leaves the element in the collection whose Add attached it, so it leaves both again.
        try
        {
            LogicalChildren.Add(element);
            VisualChildren.Add(element);
        }
        catch
        {
            VisualChildren.Remove(element);
            LogicalChildren.Remove(element);
            throw;
        }

        hostedContentElement = element;
    }

    private void RemoveContentElement(object? content)
    {
        if (content is not UIElement element)
        {
            return;
        }

        VisualChildren.Remove(element);
        LogicalChildren.Remove(element);
        if (ReferenceEquals(hostedContentElement, element))
        {
            hostedContentElement = null;
        }
    }

    private void ReleaseContentElementFromOwnedSubtree()
    {
        hostedContentElement = null;
        if (Content is UIElement element)
        {
            DetachChildFromOwnedSubtree(this, element);
        }
    }

    private static bool IsAncestor(UIElement owner, UIElement candidate, ElementChildRole role)
    {
        UIElement? current = role == ElementChildRole.Logical
            ? owner.LogicalParent
            : owner.VisualParent;

        while (current is not null)
        {
            if (ReferenceEquals(current, candidate))
            {
                return true;
            }

            current = role == ElementChildRole.Logical
                ? current.LogicalParent
                : current.VisualParent;
        }

        return false;
    }

    private static bool IsOwnedBy(UIElement? parent, UIElement owner, ElementChildRole role)
    {
        for (UIElement? current = parent; current is not null;)
        {
            if (ReferenceEquals(current, owner))
            {
                return true;
            }

            current = role == ElementChildRole.Logical
                ? current.LogicalParent
                : current.VisualParent;
        }

        return false;
    }

    private sealed class ContentValueEqualityComparer : IEqualityComparer<object?>
    {
        public new bool Equals(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is UIElement || right is UIElement)
            {
                return false;
            }

            return EqualityComparer<object?>.Default.Equals(left, right);
        }

        public int GetHashCode(object? value)
        {
            return value?.GetHashCode() ?? 0;
        }
    }
}
