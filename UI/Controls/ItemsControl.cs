using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Cerneala.UI.Aspect;
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
    private readonly ItemsPresenter fallbackItemsPresenter;
    private ItemsPresenter itemsPresenter;
    private IReadOnlyList<object?> itemsSourceSnapshot = [];
    private IObservableList? observableItemsSource;
    private INotifyCollectionChanged? notifyingItemsSource;
    private bool isItemsSourceSubscribed;
    private bool hasEverAttached;
    private ContentTemplateRegistry contentTemplateRegistry = new();

    public ItemsControl()
    {
        Items = new ItemCollection();
        Templates = new ContentTemplateCollection(RebuildTemplateRegistry);
        Items.Changed += OnItemsChanged;
        ItemContainerGenerator = new ItemContainerGenerator(this);
        fallbackItemsPresenter = new ItemsPresenter
        {
            ItemsOwner = this
        };
        itemsPresenter = fallbackItemsPresenter;
        LogicalChildren.Add(fallbackItemsPresenter);
        VisualChildren.Add(fallbackItemsPresenter);
    }

    public static readonly UiProperty<ContentTemplate?> ItemTemplateProperty = UiProperty<ContentTemplate?>.Register(
        nameof(ItemTemplate),
        typeof(ItemsControl),
        new UiPropertyMetadata<ContentTemplate?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<Layout.Panels.Panel?> ItemsPanelProperty = UiProperty<Layout.Panels.Panel?>.Register(
        nameof(ItemsPanel),
        typeof(ItemsControl),
        new UiPropertyMetadata<Layout.Panels.Panel?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<ElementAspect?> ItemContainerAspectProperty = UiProperty<ElementAspect?>.Register(
        nameof(ItemContainerAspect),
        typeof(ItemsControl),
        new UiPropertyMetadata<ElementAspect?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange |
            UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsHitTest));

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

    public static readonly UiProperty<string> DisplayMemberPathProperty = UiProperty<string>.Register(
        nameof(DisplayMemberPath),
        typeof(ItemsControl),
        new UiPropertyMetadata<string>(
            string.Empty,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsSemantics,
            validateValue: value => value is not null));

    public ItemCollection Items { get; }

    public Collection<ContentTemplate> Templates { get; }

    internal ItemContainerGenerator ItemContainerGenerator { get; }

    internal ItemsPresenter ItemsPresenter => itemsPresenter;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int ItemCount => ItemsSource is null ? Items.Count : itemsSourceSnapshot.Count;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public int RealizedItemCount => ItemContainerGenerator.RealizedContainers.Count;

    internal virtual int ViewItemCount => ItemCount;

    public string DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value ?? string.Empty);
    }

    public ContentTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public ElementAspect? ItemContainerAspect
    {
        get => GetValue(ItemContainerAspectProperty);
        set => SetValue(ItemContainerAspectProperty, value);
    }

    public string? ItemTemplateKey
    {
        get => GetValue(ItemTemplateKeyProperty);
        set => SetValue(ItemTemplateKeyProperty, value);
    }

    internal ContentTemplateRegistry ContentTemplateRegistry
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

    public Layout.Panels.Panel? ItemsPanel
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
        if (ItemsSource is not null)
        {
            return itemsSourceSnapshot[index];
        }

        return Items[index];
    }

    internal virtual int GetSourceIndexForViewIndex(int viewIndex)
    {
        return viewIndex;
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
            ReferenceEquals(args.Property, ItemsPanelProperty) ||
            ReferenceEquals(args.Property, DisplayMemberPathProperty) ||
            ReferenceEquals(args.Property, ItemContainerAspectProperty))
        {
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
            InvalidateItems("ItemsControl item policy changed");
        }
        else if (ReferenceEquals(args.Property, ItemsSourceProperty))
        {
            SubscribeItemsSource(args.OldValue as IEnumerable, args.NewValue as IEnumerable);
            OnItemsViewSourceChanged();
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
            InvalidateItems("ItemsControl items source changed");
        }
    }

    protected virtual Type DefaultContainerType => typeof(ContentPresenter);

    protected override void OnAttached()
    {
        base.OnAttached();
        bool isReattaching = hasEverAttached;
        hasEverAttached = true;
        if (isReattaching && ItemsSource is not null)
        {
            RebuildItemsSourceSnapshot(ItemsSource);
            ItemContainerGenerator.Clear();
            itemsPresenter.MarkItemsDirty();
        }

        SubscribeItemsSourceIfAttached();
    }

    protected override void OnDetached()
    {
        UnsubscribeItemsSource();
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
        ApplyItemContainerAspect(container);
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

        PrepareItemContent(container, index, item);
    }

    protected virtual void PrepareItemContent(UIElement container, int index, object? item)
    {
        switch (container)
        {
            case ContentPresenter presenter:
                presenter.Content = ItemTemplate is null ? GetItemDisplayValue(item) : item;
                presenter.ContentTemplate = ItemTemplate;
                presenter.ContentTemplateKey = ItemTemplateKey;
                presenter.LocalTemplateRegistry = ContentTemplateRegistry;
                presenter.ContentIndex = index;
                break;
            case ContentControl contentControl:
                contentControl.Content = ItemTemplate is null ? GetItemDisplayValue(item) : item;
                break;
        }
    }

    protected void ActivateItemsPresenter(ItemsPresenter? presenter)
    {
        ItemsPresenter next = presenter ?? fallbackItemsPresenter;
        if (ReferenceEquals(itemsPresenter, next))
        {
            next.ItemsOwner = this;
            return;
        }

        ItemsPresenter previous = itemsPresenter;
        previous.ItemsOwner = null;
        if (ReferenceEquals(previous.VisualParent, this))
        {
            VisualChildren.Remove(previous);
        }

        if (ReferenceEquals(previous.LogicalParent, this))
        {
            LogicalChildren.Remove(previous);
        }

        itemsPresenter = next;
        next.ItemsOwner = this;
        if (ReferenceEquals(next, fallbackItemsPresenter))
        {
            if (next.LogicalParent is null)
            {
                LogicalChildren.Add(next);
            }

            if (next.VisualParent is null)
            {
                VisualChildren.Add(next);
            }
        }

        next.MarkItemsDirty();
        InvalidateItems("Items presenter changed");
    }

    internal object? GetItemDisplayValue(object? item)
    {
        return DisplayMemberPathAccessor.Resolve(item, DisplayMemberPath);
    }

    internal string GetItemDisplayText(object? item)
    {
        return GetItemDisplayValue(item)?.ToString() ?? string.Empty;
    }

    protected internal virtual void ClearItemContainer(UIElement container)
    {
        container.ClearValue(UIElement.AspectProperty, UiPropertyValueSource.AspectBase);
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

    internal virtual void OnItemsViewSourceChanged()
    {
    }

    internal void SetVirtualizationContext(VirtualizationContext? context)
    {
        itemsPresenter.VirtualizationContext = context;
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Items virtualization context changed");
    }

    internal void UpdateVirtualizationFromScrollInfo(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)
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

        if (Items.IsResetNotification)
        {
            ItemContainerGenerator.Clear();
        }

        OnItemsViewSourceChanged();
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Items changed");
    }

    private void SubscribeItemsSource(IEnumerable? oldSource, IEnumerable? newSource)
    {
        if (ReferenceEquals(oldSource, newSource))
        {
            return;
        }

        UnsubscribeItemsSource();
        RebuildItemsSourceSnapshot(newSource);
        observableItemsSource = newSource as IObservableList;
        notifyingItemsSource = observableItemsSource is null ? newSource as INotifyCollectionChanged : null;
        SubscribeItemsSourceIfAttached();
    }

    private void SubscribeItemsSourceIfAttached()
    {
        if ((hasEverAttached && !IsAttached) || isItemsSourceSubscribed)
        {
            return;
        }

        if (observableItemsSource is not null)
        {
            observableItemsSource.Changed += OnObservableItemsSourceChanged;
            isItemsSourceSubscribed = true;
        }
        else if (notifyingItemsSource is not null)
        {
            notifyingItemsSource.CollectionChanged += OnNotifyingItemsSourceChanged;
            isItemsSourceSubscribed = true;
        }
    }

    internal void UpdateAutomaticVirtualization(float viewportExtent, float scrollOffset)
    {
        if (itemsPresenter.UpdateAutomaticVirtualization(viewportExtent, scrollOffset))
        {
            InvalidateItems("Items scroll viewport changed");
        }
    }

    private void UnsubscribeItemsSource()
    {
        if (!isItemsSourceSubscribed)
        {
            return;
        }

        if (observableItemsSource is not null)
        {
            observableItemsSource.Changed -= OnObservableItemsSourceChanged;
        }
        else if (notifyingItemsSource is not null)
        {
            notifyingItemsSource.CollectionChanged -= OnNotifyingItemsSourceChanged;
        }

        isItemsSourceSubscribed = false;
    }

    private void OnObservableItemsSourceChanged(object? sender, ObservableListChangedEventArgs args)
    {
        VerifyCollectionNotificationAccess("ObservableList");
        RebuildItemsSourceSnapshot(ItemsSource);

        if (args.Kind is ObservableListChangeKind.Reset or ObservableListChangeKind.Clear)
        {
            ItemContainerGenerator.Clear();
        }

        OnItemsViewSourceChanged();
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Observable items source changed");
    }

    private void OnNotifyingItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        VerifyCollectionNotificationAccess("INotifyCollectionChanged");
        RebuildItemsSourceSnapshot(ItemsSource);

        if (args.Action is NotifyCollectionChangedAction.Reset)
        {
            ItemContainerGenerator.Clear();
        }

        OnItemsViewSourceChanged();
        itemsPresenter.MarkItemsDirty();
        InvalidateItems("Observable items source changed");
    }

    private void RebuildItemsSourceSnapshot(IEnumerable? source)
    {
        itemsSourceSnapshot = source is null ? [] : [.. source.Cast<object?>()];
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

    private void ApplyItemContainerAspect(UIElement container)
    {
        if (ItemContainerAspect is ElementAspect aspect)
        {
            container.SetValue(UIElement.AspectProperty, aspect, UiPropertyValueSource.AspectBase);
        }
        else
        {
            container.ClearValue(UIElement.AspectProperty, UiPropertyValueSource.AspectBase);
        }
    }

    private void RebuildTemplateRegistry()
    {
        ContentTemplateRegistry registry = new();
        foreach (ContentTemplate template in Templates)
        {
            registry.Register(template);
        }

        ContentTemplateRegistry = registry;
    }

    private sealed class ContentTemplateCollection(Action changed) : Collection<ContentTemplate>
    {
        protected override void InsertItem(int index, ContentTemplate item)
        {
            ArgumentNullException.ThrowIfNull(item);
            EnsureUnique(item, ignoredIndex: -1);
            base.InsertItem(index, item);
            changed();
        }

        protected override void SetItem(int index, ContentTemplate item)
        {
            ArgumentNullException.ThrowIfNull(item);
            EnsureUnique(item, index);
            base.SetItem(index, item);
            changed();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            changed();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            changed();
        }

        private void EnsureUnique(ContentTemplate candidate, int ignoredIndex)
        {
            for (int index = 0; index < Count; index++)
            {
                ContentTemplate existing = this[index];
                if (index != ignoredIndex &&
                    existing.DataType == candidate.DataType &&
                    string.Equals(existing.Key, candidate.Key, StringComparison.Ordinal))
                {
                    string dataType = candidate.DataType?.FullName ?? "<null>";
                    string key = candidate.Key is null ? string.Empty : $" and key '{candidate.Key}'";
                    throw new InvalidOperationException(
                        $"ItemsControl already contains a template for DataType '{dataType}'{key}.");
                }
            }
        }
    }
}

public interface ISelectableItemContainer
{
    int ItemIndex { get; set; }

    object? Item { get; set; }

    bool IsSelected { get; set; }
}
