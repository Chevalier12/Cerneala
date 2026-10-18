using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Input;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;

namespace Cerneala.UI.Controls;

public class TabItem : ContentControl, ISelectableItemContainer
{
    public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(TabItem));
    public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(TabItem));

    public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }
    public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }
    public static readonly UiProperty<bool> IsSelectedProperty = UiProperty<bool>.Register(
        nameof(IsSelected),
        typeof(TabItem),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect));

    public static readonly UiProperty<object?> HeaderProperty = UiProperty<object?>.Register(
        nameof(Header),
        typeof(TabItem),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ContentTemplate?> HeaderTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(HeaderTemplate),
        typeof(TabItem),
        new UiPropertyMetadata<ContentTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<string?> HeaderTemplateKeyProperty = UiProperty<string?>.Register(
        nameof(HeaderTemplateKey),
        typeof(TabItem),
        new UiPropertyMetadata<string?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ContentTemplate?> ContentTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ContentTemplate),
        typeof(TabItem),
        new UiPropertyMetadata<ContentTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    // The header element this TabItem hosts as its own child, when Header is a UIElement that has no template.
    private UIElement? hostedHeader;

    // Presents every other header: text, and any header with a HeaderTemplate or HeaderTemplateKey.
    private ContentPresenter? headerPresenter;
    private ContentTemplateRegistry? headerTemplateRegistry;
    private int headerIndex = -1;

    public int ItemIndex { get; set; } = -1;

    public object? Item { get; set; }

    // The TabControl that shows this tab's Content; it is told when the Content or ContentTemplate changes.
    internal TabControl? Owner { get; set; }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set
        {
            object? oldHeader = Header;
            if (ContentEqualityComparer.Equals(oldHeader, value))
            {
                SetValue(HeaderProperty, value);
                return;
            }

            ValidateCanOwnChild(this, value as UIElement);
            if (HostsHeaderElement)
            {
                RemoveHeaderElement(oldHeader);
                try
                {
                    AddHeaderElement(value);
                    SetValue(HeaderProperty, value);
                }
                catch
                {
                    RemoveHeaderElement(value);
                    AddHeaderElement(oldHeader);
                    SetValue(HeaderProperty, oldHeader);
                    throw;
                }

                return;
            }

            SetValue(HeaderProperty, value);
        }
    }

    public ContentTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    public string? HeaderTemplateKey
    {
        get => GetValue(HeaderTemplateKeyProperty);
        set => SetValue(HeaderTemplateKeyProperty, value);
    }

    public ContentTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    // A TabItem never hosts its Content: the TabControl shows the Content of its selected tab in its content area.
    // A UIElement Content is still owned: it is the logical child of this TabItem whenever the content area does not
    // show it, so it cannot be added to another parent or become the Content of another tab.
    internal override bool HostsContentDirectly(object? content) => false;

    private bool IsComponentTemplated => ComponentTemplate is not null || ComponentTemplateKey is not null;

    private bool HostsHeaderElement => !IsComponentTemplated && HeaderTemplate is null && HeaderTemplateKey is null;

    // The owning TabControl passes the registry of its Templates and the item index for a generated tab.
    internal void SetHeaderPresentation(ContentTemplateRegistry? registry, int index)
    {
        headerTemplateRegistry = registry;
        headerIndex = index;
        UpdateHeaderHosting();
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        Thickness insets = Insets;
        LayoutSize available = Deflate(context.AvailableSize, insets);
        UIElement? header = hostedHeader ?? headerPresenter;
        LayoutSize headerSize = header?.Measure(new MeasureContext(available, context.Rounding)) ?? LayoutSize.Zero;
        return Inflate(headerSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        UIElement? header = hostedHeader ?? headerPresenter;
        header?.Arrange(new ArrangeContext(Deflate(context.FinalRect, Insets), context.Rounding));
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        if (ReferenceEquals(args.Property, IsSelectedProperty))
        {
            base.OnPropertyChanged(args);
            RaiseEvent(new RoutedEventArgs(IsSelected ? SelectedEvent : UnselectedEvent, this));
            return;
        }

        if (ReferenceEquals(args.Property, ComponentTemplateProperty) ||
            ReferenceEquals(args.Property, ComponentTemplateKeyProperty))
        {
            RemoveHeaderPresenter();
            ReleaseHeaderElementFromOwnedSubtree();
            hostedHeader = null;
            base.OnPropertyChanged(args);
            UpdateHeaderHosting();
            // ContentControl releases the content from this control's subtree when the template changes.
            HoldContent();
            return;
        }

        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, HeaderProperty) ||
            ReferenceEquals(args.Property, HeaderTemplateProperty) ||
            ReferenceEquals(args.Property, HeaderTemplateKeyProperty))
        {
            UpdateHeaderHosting();
        }
        else if (ReferenceEquals(args.Property, ContentProperty) ||
                 ReferenceEquals(args.Property, ContentTemplateProperty))
        {
            if (ReferenceEquals(args.Property, ContentProperty))
            {
                ReleaseHeldContent(args.OldValue as UIElement);
                HoldContent();
            }

            Owner?.OnTabContentChanged(this);
        }
    }

    // Takes a UIElement Content as a logical child when nothing else owns it. The content area of the TabControl
    // owns it while it shows it, and the TabControl calls this after the content area let go of it.
    internal void HoldContent()
    {
        if (Content is UIElement element && element.LogicalParent is null && element.VisualParent is null)
        {
            LogicalChildren.Add(element);
        }
    }

    // The TabControl calls this right before its content area shows the Content of this tab.
    internal void ReleaseContentForContentArea()
    {
        ReleaseHeldContent(Content as UIElement);
    }

    private void ReleaseHeldContent(UIElement? element)
    {
        if (element is not null && ReferenceEquals(element.LogicalParent, this))
        {
            LogicalChildren.Remove(element);
        }
    }

    // The header is hosted directly only when it is a UIElement without a template; any other header is presented
    // by the header presenter. The element leaves one host before it joins the other.
    private void UpdateHeaderHosting()
    {
        UIElement? wanted = HostsHeaderElement ? Header as UIElement : null;
        if (hostedHeader is not null && !ReferenceEquals(hostedHeader, wanted))
        {
            RemoveHeaderElement(hostedHeader);
        }

        object? header = Header;
        bool needsPresenter = !IsComponentTemplated && header is not null && wanted is null;
        if (!needsPresenter)
        {
            RemoveHeaderPresenter();
        }

        if (wanted is not null && hostedHeader is null)
        {
            AddHeaderElement(wanted);
        }

        if (!needsPresenter)
        {
            return;
        }

        if (headerPresenter is null)
        {
            ContentPresenter presenter = new();
            LogicalChildren.Add(presenter);
            VisualChildren.Add(presenter);
            headerPresenter = presenter;
        }

        headerPresenter.PrepareItemPresentation(header, HeaderTemplate, HeaderTemplateKey, headerTemplateRegistry, headerIndex);
    }

    private void RemoveHeaderPresenter()
    {
        if (headerPresenter is not { } presenter)
        {
            return;
        }

        // Clearing the content lets the presenter release its child, which can be the header element itself.
        presenter.Content = null;
        presenter.ContentTemplate = null;
        presenter.ContentTemplateKey = null;
        presenter.LocalTemplateRegistry = null;
        presenter.ContentIndex = -1;
        VisualChildren.Remove(presenter);
        LogicalChildren.Remove(presenter);
        headerPresenter = null;
    }

    private void AddHeaderElement(object? header)
    {
        if (header is not UIElement element)
        {
            return;
        }

        LogicalChildren.Add(element);
        VisualChildren.Add(element);
        hostedHeader = element;
    }

    private void RemoveHeaderElement(object? header)
    {
        if (header is not UIElement element)
        {
            return;
        }

        VisualChildren.Remove(element);
        LogicalChildren.Remove(element);
        if (ReferenceEquals(hostedHeader, element))
        {
            hostedHeader = null;
        }
    }

    private void ReleaseHeaderElementFromOwnedSubtree()
    {
        if (Header is UIElement element)
        {
            DetachChildFromOwnedSubtree(this, element);
        }
    }
}
