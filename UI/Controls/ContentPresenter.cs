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
    // Set for a presenter that presents an item: content that is neither an element nor a string, and that no template
    // handles, is presented as the text of its ToString(), as WPF shows an item without an ItemTemplate (#903).
    private bool presentsItemText;
    private bool deferPresentationRefresh;
    private int contentIndex = -1;
    // Counts refreshes, so a refresh can tell that a nested refresh replaced the presentation while it ran.
    private int presentationRefresh;
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
        int index,
        bool presentsItemText = false)
    {
        deferPresentationRefresh = true;
        try
        {
            Content = content;
            ContentTemplate = contentTemplate;
            ContentTemplateKey = contentTemplateKey;
            LocalTemplateRegistry = templateRegistry;
            ContentIndex = index;
            SetPresentsItemText(presentsItemText);
        }
        finally
        {
            deferPresentationRefresh = false;
        }
    }

    // Prepares this presenter as the generated container of an item. As in WPF's ContentPresenter.PrepareContentPresenter,
    // a null item template or key is not written, so a ContentTemplate or ContentTemplateKey from ItemContainerAspect
    // applies. The owner clears the values written here before it prepares the container again.
    internal void PrepareItemContainerPresentation(
        object? content,
        ContentTemplate? itemTemplate,
        string? itemTemplateKey,
        ContentTemplateRegistry? templateRegistry,
        int index)
    {
        deferPresentationRefresh = true;
        try
        {
            Content = content;
            if (itemTemplate is not null)
            {
                ContentTemplate = itemTemplate;
            }

            if (itemTemplateKey is not null)
            {
                ContentTemplateKey = itemTemplateKey;
            }

            LocalTemplateRegistry = templateRegistry;
            ContentIndex = index;
            SetPresentsItemText(true);
        }
        finally
        {
            deferPresentationRefresh = false;
        }
    }

    private void SetPresentsItemText(bool value)
    {
        if (presentsItemText != value)
        {
            presentsItemText = value;
            presentationDirty = true;
        }
    }

    // Clears what PrepareItemContainerPresentation wrote. Clearing a container presents no item, so, as with
    // preparation, the presented child is rebuilt only on the next measure and no template runs without its item.
    internal void ClearItemContainerPresentation()
    {
        deferPresentationRefresh = true;
        try
        {
            Content = null;
            ClearValue(ContentTemplateProperty);
            ClearValue(ContentTemplateKeyProperty);
            LocalTemplateRegistry = null;
            ContentIndex = -1;
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

        // The field is cleared first: the removal raises Unloaded, and a handler can present other content at once.
        if (ReferenceEquals(presentedChild, previousContent))
        {
            presentedChild = null;
            RemovePresentedChild(previousContent);
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
        int refresh = ++presentationRefresh;
        object? content = Content;
        OnPresentationRefreshing(content);
        try
        {
            ContentTemplate? template = ResolveContentTemplate(content);
            UIElement? previousChild = presentedChild;
            bool previousChildIsGeneratedText = generatedTextChild;
            ElementContentPlacement? releasedContent = ReleaseElementContent(content, template is not null);
            if (refresh != presentationRefresh)
            {
                return;
            }

            UIElement? next;
            try
            {
                next = CreatePresentedChild(content, template);
            }
            catch
            {
                if (refresh == presentationRefresh)
                {
                    RestorePresentation(previousChild, previousChildIsGeneratedText, releasedContent);
                }

                throw;
            }

            if (refresh != presentationRefresh)
            {
                DiscardSupersededChild(next, content);
                return;
            }

            if (ReferenceEquals(presentedChild, next))
            {
                return;
            }

            UIElement? replacedChild = presentedChild;
            presentedChild = null;
            RemovePresentedChild(replacedChild);
            if (refresh != presentationRefresh)
            {
                DiscardSupersededChild(next, content);
                return;
            }

            try
            {
                AddPresentedChild(next);
            }
            catch
            {
                if (refresh == presentationRefresh)
                {
                    DiscardRejectedChild(next);
                    RestorePresentation(previousChild, previousChildIsGeneratedText, releasedContent);
                }

                throw;
            }

            // Adding the child raises Loaded at once, and a handler can change the content in the same way.
            if (refresh != presentationRefresh)
            {
                if (!ReferenceEquals(next, presentedChild))
                {
                    DiscardRejectedChild(next);
                }

                DiscardSupersededChild(next, content);
                return;
            }

            presentedChild = next;
        }
        finally
        {
            OnPresentationRefreshed(content);
        }
    }

    // Removing an element from the presenter raises its Unloaded event at once, and a handler can change Content,
    // ContentTemplate, or ContentTemplateKey. That change runs a nested refresh, which presents the current content.
    // The outer refresh then holds a child built for content that is no longer current: it does not present that
    // child, and it takes the element content back out of it, so the element is free to be presented elsewhere.
    private void DiscardSupersededChild(UIElement? child, object? content)
    {
        if (child is not null &&
            content is UIElement element &&
            !ReferenceEquals(child, element) &&
            !ReferenceEquals(child, presentedChild))
        {
            ContentControl.DetachChildFromOwnedSubtree(child, element);
        }
    }

    // A refresh that fails keeps the previous presentation: the element content goes back to the place the refresh
    // took it from, and the previous presented child goes back into the presenter. The presentation stays dirty, so
    // the next measure or arrange tries the refresh again.
    private void RestorePresentation(
        UIElement? previousChild,
        bool previousChildIsGeneratedText,
        ElementContentPlacement? releasedContent)
    {
        presentationDirty = true;
        releasedContent?.Restore();
        if (previousChild is not null && !ReferenceEquals(previousChild.VisualParent, this))
        {
            AddPresentedChild(previousChild);
        }

        presentedChild = previousChild;
        generatedTextChild = previousChildIsGeneratedText;
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

        string? text = content switch
        {
            string value => value,
            not null when presentsItemText => content.ToString() ?? string.Empty,
            _ => null
        };
        if (text is not null)
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
        presentedChild = null;
        // A move to a new owner is not an exit: the element leaves at once.
        DiscardRejectedChild(child);
    }

    // Another owner takes the element that this presenter presents directly: the element leaves at once, without a
    // presence exit, so the new owner can add it, and Content becomes null. The field is cleared first: the removal
    // raises Unloaded, and content that a handler presents then stays.
    internal void ReleaseContentForNewOwner()
    {
        UIElement? child = presentedChild;
        presentedChild = null;
        DiscardRejectedChild(child);
        if (child is not null && ReferenceEquals(Content, child))
        {
            Content = null;
        }
    }

    private void RemovePresentedChild(UIElement? child)
    {
        if (child is null)
        {
            return;
        }

        // Logical first: the child stays attached through its visual parent, so the visual removal can start a
        // presence exit. In the other order the logical removal detaches the child and cancels that exit.
        LogicalChildren.Remove(child);
        VisualChildren.Remove(child);
    }

    // A child that never became the presented child, or that a new owner takes, leaves at once without a presence
    // exit: the visual removal first, so the logical removal detaches it and cancels the exit that the visual removal
    // started.
    private void DiscardRejectedChild(UIElement? child)
    {
        if (child is null)
        {
            return;
        }

        VisualChildren.Remove(child);
        LogicalChildren.Remove(child);
    }

    // Where element content was in the tree before a refresh released it. A Decorator names its child in Child, a
    // ContentControl that hosts its content directly or a ContentPresenter that presents it directly names it in
    // Content, and a TabItem that hosts its header directly names it in Header; the release goes through that property
    // (ContentControl.DetachChildFromOwnedSubtree), so the rollback goes through it too, and each Decorator,
    // ContentControl, ContentPresenter or TabItem involved keeps the property in step with its children.
    private readonly record struct ElementContentPlacement(
        UIElement Element,
        UIElement? VisualParent,
        int VisualIndex,
        UIElement? LogicalParent,
        int LogicalIndex,
        NamedChildSlot NamedSlot)
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
                logicalParent?.LogicalChildren.IndexOf(element) ?? -1,
                SlotNaming(visualParent, element));
        }

        // A template that failed after it took the element still holds it, so the element leaves that template's
        // element first. The template can also have changed the previous parent's children, so the old index is
        // limited to the current count. The visual tree comes first, because a Panel takes a logical child only
        // when it is already its visual child.
        public void Restore()
        {
            bool restoreVisual = !ReferenceEquals(Element.VisualParent, VisualParent);
            bool restoreLogical = !ReferenceEquals(Element.LogicalParent, LogicalParent);
            if (restoreVisual && SlotNaming(Element.VisualParent, Element) is var currentSlot and not NamedChildSlot.None)
            {
                SetNamedChild(Element.VisualParent!, currentSlot, null);
            }

            if (restoreVisual)
            {
                Element.VisualParent?.VisualChildren.Remove(Element);
            }

            if (restoreLogical)
            {
                Element.LogicalParent?.LogicalChildren.Remove(Element);
            }

            if (NamedSlot != NamedChildSlot.None && restoreVisual && restoreLogical && ReferenceEquals(VisualParent, LogicalParent))
            {
                SetNamedChild(VisualParent!, NamedSlot, Element);
                return;
            }

            if (restoreVisual && VisualParent is not null)
            {
                VisualParent.VisualChildren.Insert(Math.Min(VisualIndex, VisualParent.VisualChildren.Count), Element);
            }

            if (restoreLogical && LogicalParent is not null)
            {
                LogicalParent.LogicalChildren.Insert(Math.Min(LogicalIndex, LogicalParent.LogicalChildren.Count), Element);
            }
        }

        private static NamedChildSlot SlotNaming(UIElement? parent, UIElement element)
        {
            return parent switch
            {
                Decorator decorator when ReferenceEquals(decorator.Child, element) => NamedChildSlot.DecoratorChild,
                TabItem tabItem when tabItem.HostsHeaderDirectly(element) => NamedChildSlot.TabItemHeader,
                ContentControl contentControl when contentControl.HostsContentDirectly(element) &&
                    ReferenceEquals(contentControl.Content, element) => NamedChildSlot.ContentControlContent,
                ContentPresenter presenter when ReferenceEquals(presenter.PresentedChild, element) &&
                    ReferenceEquals(presenter.Content, element) => NamedChildSlot.PresenterContent,
                _ => NamedChildSlot.None
            };
        }

        private static void SetNamedChild(UIElement parent, NamedChildSlot slot, UIElement? element)
        {
            switch (slot)
            {
                case NamedChildSlot.DecoratorChild:
                    ((Decorator)parent).Child = element;
                    break;
                case NamedChildSlot.TabItemHeader:
                    ((TabItem)parent).Header = element;
                    break;
                case NamedChildSlot.ContentControlContent:
                    ((ContentControl)parent).Content = element;
                    break;
                case NamedChildSlot.PresenterContent:
                    ((ContentPresenter)parent).Content = element;
                    break;
            }
        }
    }

    // The property through which a parent names an element child.
    private enum NamedChildSlot
    {
        None,
        DecoratorChild,
        ContentControlContent,
        PresenterContent,
        TabItemHeader
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
