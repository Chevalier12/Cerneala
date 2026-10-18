using System.Runtime.CompilerServices;
using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public class ContentPresenter : Control
{
    private static readonly IEqualityComparer<object?> ContentReferenceEqualityComparer = new ReferenceContentEqualityComparer();
    private UIElement? presentedChild;
    private UIElement? contentReleasableByTemplate;
    private bool presentationDirty = true;
    private bool generatedTextChild;
    private bool deferPresentationRefresh;
    private int contentIndex = -1;
    private AspectCatalog? aspectTemplateCatalog;
    private ContentTemplateRegistry? localTemplateRegistry;

    public static readonly UiProperty<object?> ContentProperty = UiProperty<object?>.Register(
        nameof(Content),
        typeof(ContentPresenter),
        new UiPropertyMetadata<object?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            ContentReferenceEqualityComparer));

    public static readonly UiProperty<ContentTemplate?> ContentTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ContentTemplate),
        typeof(ContentPresenter),
        new UiPropertyMetadata<ContentTemplate?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<string?> ContentTemplateKeyProperty = UiProperty<string?>.Register(
        nameof(ContentTemplateKey),
        typeof(ContentPresenter),
        new UiPropertyMetadata<string?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public ContentTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public string? ContentTemplateKey
    {
        get => GetValue(ContentTemplateKeyProperty);
        set => SetValue(ContentTemplateKeyProperty, value);
    }

    public ContentTemplateRegistry? LocalTemplateRegistry
    {
        get => localTemplateRegistry;
        set
        {
            if (ReferenceEquals(localTemplateRegistry, value))
            {
                return;
            }

            localTemplateRegistry = value;
            presentationDirty = true;
            if (!deferPresentationRefresh)
            {
                RefreshPresentedChild();
            }

            Invalidate(
                InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render,
                "Content presenter local template registry changed");
        }
    }

    public ResourceId<FontResource>? FontResourceId { get; set; }

    public IResourceProvider? ResourceProvider { get; set; }

    public int ContentIndex
    {
        get => contentIndex;
        set
        {
            if (contentIndex == value)
            {
                return;
            }

            contentIndex = value;
            presentationDirty = true;
        }
    }

    public UIElement? PresentedChild => presentedChild;

    internal void PrepareItemPresentation(
        object? content,
        ContentTemplate? contentTemplate,
        string? contentTemplateKey,
        ContentTemplateRegistry? templateRegistry,
        int index)
    {
        deferPresentationRefresh = true;
        try
        {
            Content = content;
            ContentTemplate = contentTemplate;
            ContentTemplateKey = contentTemplateKey;
            LocalTemplateRegistry = templateRegistry;
            ContentIndex = index;
        }
        finally
        {
            deferPresentationRefresh = false;
        }
    }

    internal void RefreshAspectContentTemplates(AspectCatalog catalog)
    {
        if (ReferenceEquals(aspectTemplateCatalog, catalog))
        {
            return;
        }

        aspectTemplateCatalog = catalog;
        if (ContentTemplate is not null || LocalTemplateRegistry is not null)
        {
            return;
        }

        presentationDirty = true;
        RefreshPresentedChild();
        Invalidate(
            InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render,
            "Aspect content template catalog changed");
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        RefreshPresentedChild();
        return presentedChild?.Measure(context) ?? LayoutSize.Zero;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        RefreshPresentedChild();
        presentedChild?.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ContentProperty))
        {
            ReleaseFromPresentedChild(args.OldValue as UIElement);
        }

        if (ReferenceEquals(args.Property, ContentProperty) ||
            ReferenceEquals(args.Property, ContentTemplateProperty) ||
            ReferenceEquals(args.Property, ContentTemplateKeyProperty))
        {
            presentationDirty = true;
            if (!deferPresentationRefresh)
            {
                RefreshPresentedChild();
            }
        }

        if (generatedTextChild &&
            presentedChild is TextBlock textBlock &&
            (ReferenceEquals(args.Property, ForegroundProperty) ||
             ReferenceEquals(args.Property, FontFamilyProperty) ||
             ReferenceEquals(args.Property, FontSizeProperty)))
        {
            ApplyGeneratedTextAspect(textBlock);
        }
    }

    // A template can place an element content inside the element it creates, or return the element itself. When that
    // element stops being the content, the presenter lets go of it at once, even when the presented child is rebuilt
    // only on the next measure, so the element can be presented elsewhere before that, for example by a recycled item
    // container.
    private void ReleaseFromPresentedChild(UIElement? previousContent)
    {
        if (previousContent is null || presentedChild is null)
        {
            return;
        }

        if (ReferenceEquals(presentedChild, previousContent))
        {
            RemovePresentedChild(previousContent);
            presentedChild = null;
        }
        else
        {
            ContentControl.DetachChildFromOwnedSubtree(this, previousContent);
        }
    }

    private void RefreshPresentedChild()
    {
        if (!presentationDirty)
        {
            return;
        }

        presentationDirty = false;
        object? content = Content;
        OnPresentationRefreshing(content);
        try
        {
            ContentTemplate? template = ResolveContentTemplate(content);
            UIElement? previousChild = presentedChild;
            ElementContentPlacement? releasedContent = ReleaseElementContent(content, template is not null);
            UIElement? next;
            try
            {
                next = CreatePresentedChild(content, template);
            }
            catch
            {
                RestorePresentation(previousChild, releasedContent);
                throw;
            }

            if (ReferenceEquals(presentedChild, next))
            {
                return;
            }

            RemovePresentedChild(presentedChild);
            try
            {
                AddPresentedChild(next);
                presentedChild = next;
            }
            catch
            {
                RemovePresentedChild(next);
                RestorePresentation(previousChild, releasedContent);
                throw;
            }
        }
        finally
        {
            OnPresentationRefreshed(content);
        }
    }

    // A refresh that fails keeps the previous presentation: the element content goes back to the place the refresh
    // took it from, and the previous presented child goes back into the presenter.
    private void RestorePresentation(UIElement? previousChild, ElementContentPlacement? releasedContent)
    {
        releasedContent?.Restore();
        if (previousChild is not null && !ReferenceEquals(previousChild.VisualParent, this))
        {
            AddPresentedChild(previousChild);
        }

        presentedChild = previousChild;
    }

    // A presenter whose element content is owned elsewhere while it is not shown, for example the content area of a
    // TabControl, lets that owner release the content before the presentation is rebuilt, so the presenter or its
    // template can take it, and take it back afterwards when the new presentation did not take it.
    internal virtual void OnPresentationRefreshing(object? content)
    {
    }

    internal virtual void OnPresentationRefreshed(object? content)
    {
    }

    private ContentTemplate? ResolveContentTemplate(object? content)
    {
        if (ContentTemplate is { } template)
        {
            return template;
        }

        ContentTemplateRegistry? registry = LocalTemplateRegistry ?? Root?.AspectProcessor.GetContentTemplates(this);
        return registry is not null &&
            registry.TryResolve(new ContentTemplateMatchContext(content, ContentTemplateKey, this), out ContentTemplate? resolved)
                ? resolved
                : null;
    }

    // A template can place element content inside the element it creates. Before the next child is built, the element
    // content leaves the current presented child that wraps it, so the next template or the presenter itself can take
    // it. Element content that the presenter hosts directly stays in place until a template adds it to another element.
    // The returned placement lets a failed refresh give the element back.
    private ElementContentPlacement? ReleaseElementContent(object? content, bool templated)
    {
        if (content is not UIElement element || presentedChild is null)
        {
            return null;
        }

        if (!ReferenceEquals(presentedChild, element))
        {
            ElementContentPlacement placement = ElementContentPlacement.Capture(element);
            ContentControl.DetachChildFromOwnedSubtree(this, element);
            return placement;
        }

        // A template can still take the directly hosted element through ReleaseChildForNewOwner and then fail.
        return templated ? ElementContentPlacement.Capture(element) : null;
    }

    private UIElement? CreatePresentedChild(object? content, ContentTemplate? template)
    {
        if (template is not null)
        {
            generatedTextChild = false;
            // A template that returns the hosted element itself keeps it in place; one that adds it to the element it
            // creates takes it from the presenter through ReleaseChildForNewOwner.
            contentReleasableByTemplate = ReferenceEquals(presentedChild, content) ? presentedChild : null;
            try
            {
                return template.Create(CreateTemplateContext(content));
            }
            finally
            {
                contentReleasableByTemplate = null;
            }
        }

        if (content is UIElement element)
        {
            generatedTextChild = false;
            return element;
        }

        if (content is string text)
        {
            if (generatedTextChild && presentedChild is TextBlock textBlock)
            {
                textBlock.Text = text;
                ApplyGeneratedTextAspect(textBlock);
                return textBlock;
            }

            generatedTextChild = true;
            TextBlock generated = new() { Text = text };
            ApplyGeneratedTextAspect(generated);
            return generated;
        }

        generatedTextChild = false;
        return null;
    }

    private ContentTemplateContext CreateTemplateContext(object? content)
    {
        AspectVariantSet variants = AspectVariants;
        object owner = this;
        if (TemplateAspectContext.TryGet(this, out TemplateAspectContext.Registration registration))
        {
            variants = registration.Owner.AspectVariants;
            owner = registration.Owner;
        }

        return new ContentTemplateContext(
            content,
            this,
            Root?.AspectProcessor.GetEnvironment(this),
            variants,
            ContentIndex,
            owner);
    }

    private void ApplyGeneratedTextAspect(TextBlock textBlock)
    {
        textBlock.FontFamily = FontFamily;
        textBlock.FontSize = FontSize;
        textBlock.Foreground = Foreground;
        textBlock.ResourceProvider = ResourceProvider;
        textBlock.FontResourceId = FontResourceId;
    }

    private void AddPresentedChild(UIElement? child)
    {
        if (child is null)
        {
            return;
        }

        ContentControl.ValidateCanOwnChild(this, child);
        LogicalChildren.Add(child);
        try
        {
            VisualChildren.Add(child);
        }
        catch
        {
            LogicalChildren.Remove(child);
            throw;
        }
    }

    internal override void ReleaseChildForNewOwner(UIElement child)
    {
        if (!ReferenceEquals(child, contentReleasableByTemplate))
        {
            return;
        }

        contentReleasableByTemplate = null;
        RemovePresentedChild(child);
        presentedChild = null;
    }

    private void RemovePresentedChild(UIElement? child)
    {
        if (child is null)
        {
            return;
        }

        VisualChildren.Remove(child);
        LogicalChildren.Remove(child);
    }

    // Where element content was in the tree before a refresh released it.
    private readonly record struct ElementContentPlacement(
        UIElement Element,
        UIElement? VisualParent,
        int VisualIndex,
        UIElement? LogicalParent,
        int LogicalIndex)
    {
        public static ElementContentPlacement Capture(UIElement element)
        {
            UIElement? visualParent = element.VisualParent;
            UIElement? logicalParent = element.LogicalParent;
            return new ElementContentPlacement(
                element,
                visualParent,
                visualParent?.VisualChildren.IndexOf(element) ?? -1,
                logicalParent,
                logicalParent?.LogicalChildren.IndexOf(element) ?? -1);
        }

        // A template that failed after it took the element still holds it, so the element leaves that template's
        // element first.
        public void Restore()
        {
            bool restoreVisual = !ReferenceEquals(Element.VisualParent, VisualParent);
            bool restoreLogical = !ReferenceEquals(Element.LogicalParent, LogicalParent);
            if (restoreVisual)
            {
                Element.VisualParent?.VisualChildren.Remove(Element);
            }

            if (restoreLogical)
            {
                Element.LogicalParent?.LogicalChildren.Remove(Element);
                LogicalParent?.LogicalChildren.Insert(LogicalIndex, Element);
            }

            if (restoreVisual)
            {
                VisualParent?.VisualChildren.Insert(VisualIndex, Element);
            }
        }
    }

    private sealed class ReferenceContentEqualityComparer : IEqualityComparer<object?>
    {
        public new bool Equals(object? left, object? right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(object? value)
        {
            return value is null ? 0 : RuntimeHelpers.GetHashCode(value);
        }
    }
}
