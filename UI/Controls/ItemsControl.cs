using System.Collections;
using Cerneala.UI.Core;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.UI.Controls;

public class ItemsControl : Control
{
    private readonly ItemsPresenter itemsPresenter;
    private IObservableList? observableItemsSource;
    private bool isObservableItemsSourceSubscribed;
    private bool hasEverAttached;
    private ContentTemplateRegistry contentTemplateRegistry = new();

    public ItemsControl()
    {
        Items = new ItemCollection();
        Items.Changed += OnItemsChanged;
        ItemContainerGenerator = new ItemContainerGenerator(this);
        itemsPresenter = new ItemsPresenter
        {
            ItemsOwner = this
        };
        LogicalChildren.Add(itemsPresenter);
        VisualChildren.Add(itemsPresenter);
    }

    public static readonly UiProperty<ContentTemplate?> ItemTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ItemTemplate),
        typeof(ItemsControl),
        new UiPropertyMetadata<ContentTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ItemsPanelTemplate?> ItemsPanelProperty = UiProperty<ItemsPanelTemplate?>.Register(
        nameof(ItemsPanel),
        typeof(ItemsControl),
        new UiPropertyMetadata<ItemsPanelTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<string?> ItemTemplateKeyProperty = UiProperty<string?>.Register(
        nameof(ItemTemplateKey),
        typeof(ItemsControl),
        new UiPropertyMetadata<string?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<IEnumerable?> ItemsSourceProperty = UiProperty<IEnumerable?>.Register(
        nameof(ItemsSource),
        typeof(ItemsControl),
        new UiPropertyMetadata<IEnumerable?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest | UiPropertyOptions.AffectsSemantics));

    public ItemCollection Items { get; }

    public ItemContainerGenerator ItemContainerGenerator { get; }

    public ItemsPresenter ItemsPresenter => itemsPresenter;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int ItemCount => observableItemsSource?.Count ?? ItemsSource?.Cast<object?>().Count() ?? Items.Count;

    public ContentTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public string? ItemTemplateKey
    {
        get => GetValue(ItemTemplateKeyProperty);
        set => SetValue(ItemTemplateKeyProperty, value);
    }

    public ContentTemplateRegistry ContentTemplateRegistry
    {
        get => contentTemplateRegistry;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(contentTemplateRegistry, value))
            {
                return;
            }

            contentTemplateRegistry = value;
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
            InvalidateItems("ItemsControl content template registry changed");
        }
    }

    public ItemsPanelTemplate? ItemsPanel
    {
        get => GetValue(ItemsPanelProperty);
        set => SetValue(ItemsPanelProperty, value);
    }

    public void SetItems(IEnumerable? items)
    {
        Items.ReplaceWith(items);
    }

    public object? GetItemAt(int index)
    {
        if (observableItemsSource is not null)
        {
            return observableItemsSource[index];
        }

        if (ItemsSource is not null)
        {
            return ItemsSource.Cast<object?>().ElementAt(index);
        }

        return Items[index];
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        return itemsPresenter.Measure(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        itemsPresenter.Arrange(context);
        return context.FinalRect;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ItemTemplateProperty) ||
            ReferenceEquals(args.Property, ItemTemplateKeyProperty) ||
            ReferenceEquals(args.Property, ItemsPanelProperty))
        {
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
            InvalidateItems("ItemsControl item policy changed");
        }
        else if (ReferenceEquals(args.Property, ItemsSourceProperty))
        {
            SubscribeItemsSource(args.OldValue as IEnumerable, args.NewValue as IEnumerable);
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
            InvalidateItems("ItemsControl items source changed");
        }
    }

    protected virtual Type DefaultContainerType => typeof(ContentPresenter);

    protected override void OnAttached()
    {
        base.OnAttached();
        hasEverAttached = true;
        SubscribeObservableItemsSourceIfAttached();
    }

    protected override void OnDetached()
    {
        UnsubscribeObservableItemsSource();
        base.OnDetached();
    }

    protected internal virtual Type GetContainerTypeForItem(object? item)
    {
        if (ItemTemplate is not null)
        {
            return DefaultContainerType;
        }

        return item is UIElement element ? element.GetType() : DefaultContainerType;
    }

    protected internal virtual UIElement CreateItemContainer(int index, object? item)
    {
        if (ItemTemplate is not null)
        {
            return new ContentPresenter();
        }

        return item is UIElement element ? element : new ContentPresenter();
    }

    protected internal virtual void PrepareItemContainer(UIElement container, int index, object? item)
    {
        bool selected = IsItemSelected(index);
        ItemContainerGenerator.SetInfo(container, index, item, selected);
        if (container is ISelectableItemContainer selectable)
        {
            selectable.ItemIndex = index;
            selectable.Item = item;
            selectable.IsSelected = selected;
        }

        if (ReferenceEquals(container, item))
        {
            return;
        }

        switch (container)
        {
            case ContentPresenter presenter:
                presenter.Content = item;
                presenter.ContentTemplate = ItemTemplate;
                presenter.ContentTemplateKey = ItemTemplateKey;
                presenter.LocalTemplateRegistry = ContentTemplateRegistry;
                presenter.ContentIndex = index;
                break;
            case ContentControl contentControl:
                contentControl.Content = item;
                break;
        }
    }

    protected internal virtual void ClearItemContainer(UIElement container)
    {
        ItemContainerGenerator.ClearInfo(container);
        if (container is ISelectableItemContainer selectable)
        {
            selectable.ItemIndex = -1;
            selectable.Item = null;
            selectable.IsSelected = false;
        }

        switch (container)
        {
            case ContentPresenter presenter:
                presenter.Content = null;
                presenter.ContentTemplate = null;
                presenter.ContentTemplateKey = null;
                presenter.LocalTemplateRegistry = null;
                presenter.ContentIndex = -1;
                break;
            case ContentControl contentControl:
                contentControl.Content = null;
                break;
        }
    }

    protected internal virtual bool IsItemSelected(int index)
    {
        return false;
    }

    protected internal virtual void OnItemContainerPrepared(UIElement container, int index)
    {
    }

    public void SetVirtualizationContext(VirtualizationContext? context)
    {
        itemsPresenter.VirtualizationContext = context;
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Items virtualization context changed");
    }

    public void UpdateVirtualizationFromScrollInfo(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)
    {
        if (itemsPresenter.UpdateVirtualizationFromScrollInfoCore(scrollInfo, itemExtent, cacheItems))
        {
            InvalidateItems("Items scroll virtualization changed");
        }
    }

    internal void InvalidateItems(string reason)
    {
        IncrementLayoutVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest | InvalidationFlags.Semantics, reason);
    }

    private void OnItemsChanged(object? sender, EventArgs args)
    {
        VerifyCollectionNotificationAccess("ItemCollection");
        if (ItemsSource is not null)
        {
            return;
        }

        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Items changed");
    }

    private void SubscribeItemsSource(IEnumerable? oldSource, IEnumerable? newSource)
    {
        if (ReferenceEquals(oldSource, newSource))
        {
            return;
        }

        UnsubscribeObservableItemsSource();

        observableItemsSource = newSource as IObservableList;
        SubscribeObservableItemsSourceIfAttached();
    }

    private void SubscribeObservableItemsSourceIfAttached()
    {
        if ((hasEverAttached && !IsAttached) || observableItemsSource is null || isObservableItemsSourceSubscribed)
        {
            return;
        }

        observableItemsSource.Changed += OnObservableItemsSourceChanged;
        isObservableItemsSourceSubscribed = true;
    }

    private void UnsubscribeObservableItemsSource()
    {
        if (observableItemsSource is null || !isObservableItemsSourceSubscribed)
        {
            return;
        }

        observableItemsSource.Changed -= OnObservableItemsSourceChanged;
        isObservableItemsSourceSubscribed = false;
    }

    private void OnObservableItemsSourceChanged(object? sender, ObservableListChangedEventArgs args)
    {
        VerifyCollectionNotificationAccess("ObservableList");

        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Observable items source changed");
    }

    private void VerifyCollectionNotificationAccess(string collectionName)
    {
        if (Root is UIRoot root && !root.Relay.CheckAccess())
        {
            throw new InvalidOperationException(
                $"{collectionName} changes observed by an attached ItemsControl must run on the owning UI thread. " +
                "Use await root.Relay.InvokeAsync(() => items.Add(item)).");
        }
    }
}

public interface ISelectableItemContainer
{
    int ItemIndex { get; set; }

    object? Item { get; set; }

    bool IsSelected { get; set; }
}
