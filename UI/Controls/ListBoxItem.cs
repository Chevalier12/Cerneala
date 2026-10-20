using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Input;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.UI.Controls;

public class ListBoxItem : ContentControl, ISelectableItemContainer
{
    // Presents Content that is not a UIElement, as the ContentPresenter in the template of a WPF ListBoxItem does.
    // UIElement content stays the direct child that ContentControl hosts, unless a template applies to it: the item
    // template of the owning ListBox, or a template of its registry for the content and the item template key. Then
    // the presenter shows the template's element for it, as a WPF ContentPresenter tries its ContentTemplate and a
    // DataTemplate for the content before UIElement content.
    private ContentPresenter? contentPresenter;
    private ContentTemplate? itemTemplate;
    private string? itemTemplateKey;
    private ContentTemplateRegistry? itemTemplateRegistry;
    private int itemContentIndex = -1;

    public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(ListBoxItem));
    public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(ListBoxItem));

    public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }
    public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }
    public static readonly UiProperty<bool> IsSelectedProperty = UiProperty<bool>.Register(
        nameof(IsSelected),
        typeof(ListBoxItem),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect));

    // Focusable as a WPF ListBoxItem, a Control that focuses itself when it handles MouseLeftButtonDown: a press on the
    // row moves keyboard focus to it (#662). Not a Tab stop, as a WPF ListBox lets the Tab key into its list only once.
    public ListBoxItem()
    {
        Focusable = true;
        UpdateContentPresenter();
    }

    public int ItemIndex { get; set; } = -1;

    public object? Item { get; set; }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    // The owning ListBox passes its item template here, as WPF makes ItemTemplate the container's ContentTemplate.
    internal void SetItemPresentation(
        ContentTemplate? template,
        string? templateKey,
        ContentTemplateRegistry? templateRegistry,
        int index)
    {
        itemTemplate = template;
        itemTemplateKey = templateKey;
        itemTemplateRegistry = templateRegistry;
        itemContentIndex = index;
        UpdateContentHosting();
        UpdateContentPresenter();
    }

    // Only UIElement content is ever a direct child, so other content skips the template lookup.
    internal override bool HostsContentDirectly(object? content) =>
        base.HostsContentDirectly(content) && !(content is UIElement && HasItemTemplateFor(content));

    // The same order as ContentPresenter.ResolveContentTemplate: the item template, then the registry for the content
    // and the key.
    private bool HasItemTemplateFor(object? content) =>
        itemTemplate is not null ||
        (itemTemplateRegistry is not null &&
            itemTemplateRegistry.TryResolve(
                new ContentTemplateMatchContext(content, itemTemplateKey, contentPresenter),
                out _));

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null || contentPresenter is null)
        {
            return base.MeasureCore(context);
        }

        Thickness insets = Insets;
        LayoutSize available = Deflate(context.AvailableSize, insets);
        LayoutSize contentSize = contentPresenter.Measure(new MeasureContext(available, context.Rounding));
        return Inflate(contentSize, insets);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null || contentPresenter is null)
        {
            return base.ArrangeCore(context);
        }

        contentPresenter.Arrange(new ArrangeContext(Deflate(context.FinalRect, Insets), context.Rounding));
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsSelectedProperty))
        {
            RaiseEvent(new RoutedEventArgs(IsSelected ? SelectedEvent : UnselectedEvent, this));
        }
        else if (ReferenceEquals(args.Property, ContentProperty) ||
                 ReferenceEquals(args.Property, ComponentTemplateProperty) ||
                 ReferenceEquals(args.Property, ComponentTemplateKeyProperty))
        {
            UpdateContentPresenter();
        }
    }

    // The presenter refreshes its child on its next measure, so a recycled row whose content goes to null and
    // then to the next string reuses its generated TextBlock. It also exists for null content, as the
    // ContentPresenter in WPF's ListBoxItem template always does.
    private void UpdateContentPresenter()
    {
        object? content = Content;
        if ((content is UIElement && !HasItemTemplateFor(content)) || ComponentTemplate is not null || ComponentTemplateKey is not null)
        {
            RemoveContentPresenter();
            return;
        }

        if (contentPresenter is null)
        {
            ContentPresenter presenter = new();
            LogicalChildren.Add(presenter);
            VisualChildren.Add(presenter);
            contentPresenter = presenter;
        }

        contentPresenter.PrepareItemPresentation(
            content,
            itemTemplate,
            itemTemplateKey,
            itemTemplateRegistry,
            itemContentIndex);
    }

    private void RemoveContentPresenter()
    {
        if (contentPresenter is null)
        {
            return;
        }

        VisualChildren.Remove(contentPresenter);
        LogicalChildren.Remove(contentPresenter);
        contentPresenter = null;
    }

    protected override void OnRender(RenderContext context)
    {
        Brush? background = IsSelected ? new SolidColorBrush(new Color(80, 130, 220)) : Background;
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (background is not null && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }
    }
}
