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

    private Layout.Panels.VirtualizingStackPanel? MeasuredItemsPanel =>
        itemsPresenter.LayoutPanelRoot as Layout.Panels.VirtualizingStackPanel;

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
            ItemContainerGenerator.Clear();
            NotifyItemsReset();
            OnItemsViewSourceChanged();
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
        // The source is not observed while detached, so read it again; the realized containers stay when it
        // still holds the same items.
        if (isReattaching && ItemsSource is not null && RefreshItemsSourceSnapshot(ItemsSource))
        {
            ItemContainerGenerator.Clear();
            NotifyItemsReset();
            OnItemsViewSourceChanged();
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
        return IsItemItsOwnContainer(item) ? item!.GetType() : DefaultContainerType;
    }

    protected internal virtual UIElement CreateItemContainer(int index, object? item)
    {
        return IsItemItsOwnContainer(item) ? (UIElement)item! : new ContentPresenter();
    }

    // True when CreateItemContainer returns the item itself. The generator asks the owner instead of comparing
    // types: with an ItemTemplate, a ContentPresenter item has the container type but is still wrapped.
    internal virtual bool IsItemItsOwnContainer(object? item)
    {
        return ItemTemplate is null && item is UIElement;
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
                presenter.PrepareItemContainerPresentation(
                    ItemTemplate is null ? GetItemDisplayValue(item) : item,
                    ItemTemplate,
                    ItemTemplateKey,
                    ContentTemplateRegistry,
                    index);
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

    protected internal virtual void ClearItemContainer(UIElement container)
    {
        object? item = ItemContainerGenerator.GetItem(container);
        container.ClearValue(UIElement.AspectProperty, UiPropertyValueSource.TemplateBinding);
        ItemContainerGenerator.ClearInfo(container);
        if (container is ISelectableItemContainer selectable)
        {
            selectable.ItemIndex = -1;
            selectable.Item = null;
            selectable.IsSelected = false;
        }

        if (ReferenceEquals(container, item))
        {
            return;
        }

        switch (container)
        {
            case ContentPresenter presenter:
                presenter.ClearItemContainerPresentation();
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

    // The item view already reflects each change below when it is reported.
    internal virtual void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
    }

    internal virtual void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
    }

    internal virtual void OnItemsReset()
    {
    }

    // Raised before the active presenter refreshes for its new ItemsPanel, so an owner policy that
    // depends on the resolved panel applies to that refresh.
    internal virtual void OnItemsPresenterPanelChanged()
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

        try
        {
            if (Items.IsResetNotification)
            {
                ItemContainerGenerator.Clear();
                NotifyItemsReset();
            }
            else
            {
                NotifyItemsReplaced(Items.ChangeIndex, Items.ChangeRemovedCount, Items.ChangeAddedCount);
            }
        }
        finally
        {
            CompleteItemsChange("Items changed");
        }
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

    internal void ClearAutomaticVirtualization()
    {
        itemsPresenter.ClearAutomaticViewport();
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

        try
        {
            switch (args.Kind)
            {
                case ObservableListChangeKind.Add:
                    NotifyItemsReplaced(args.Index, 0, args.Items.Count);
                    break;
                case ObservableListChangeKind.Remove:
                    NotifyItemsReplaced(args.Index, args.OldItems.Count, 0);
                    break;
                case ObservableListChangeKind.Replace:
                    NotifyItemsReplaced(args.Index, args.OldItems.Count, args.Items.Count);
                    break;
                case ObservableListChangeKind.Move:
                    NotifyItemsMoved(args.OldIndex, args.Index, args.Items.Count);
                    break;
                default:
                    NotifyItemsReset();
                    break;
            }
        }
        finally
        {
            CompleteItemsChange("Observable items source changed");
        }
    }

    private void OnNotifyingItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        VerifyCollectionNotificationAccess("INotifyCollectionChanged");
        RebuildItemsSourceSnapshot(ItemsSource);

        if (args.Action is NotifyCollectionChangedAction.Reset)
        {
            ItemContainerGenerator.Clear();
        }

        try
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    NotifyItemsReplaced(args.NewStartingIndex, 0, args.NewItems?.Count ?? -1);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    NotifyItemsReplaced(args.OldStartingIndex, args.OldItems?.Count ?? -1, 0);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    NotifyItemsReplaced(args.NewStartingIndex, args.OldItems?.Count ?? -1, args.NewItems?.Count ?? -1);
                    break;
                case NotifyCollectionChangedAction.Move:
                    NotifyItemsMoved(args.OldStartingIndex, args.NewStartingIndex, args.OldItems?.Count ?? -1);
                    break;
                default:
                    NotifyItemsReset();
                    break;
            }
        }
        finally
        {
            CompleteItemsChange("Observable items source changed");
        }
    }

    // A notification above can throw, for example while a selection that follows its item prepares its
    // container. The item view, the presenter and the layout still follow the change, so the exception reaches
    // the caller and the next layout pass realizes the current items.
    private void CompleteItemsChange(string reason)
    {
        OnItemsViewSourceChanged();
        itemsPresenter.MarkItemsDirty();
        InvalidateItems(reason);
    }

    // The realized containers, the panel and the selection keep state by item index; a change whose position
    // is unknown resets them. Containers follow first, so a selection that follows its item refreshes the
    // container now at its index.
    private void NotifyItemsReplaced(int index, int removedCount, int addedCount)
    {
        if (index < 0 || removedCount < 0 || addedCount < 0)
        {
            ItemContainerGenerator.Clear();
            NotifyItemsReset();
            return;
        }

        ItemContainerGenerator.OnItemsReplaced(index, removedCount, addedCount);
        MeasuredItemsPanel?.OnItemsReplaced(index, removedCount, addedCount);
        OnItemsReplaced(index, removedCount, addedCount);
    }

    private void NotifyItemsMoved(int oldIndex, int newIndex, int count)
    {
        if (oldIndex < 0 || newIndex < 0 || count <= 0)
        {
            ItemContainerGenerator.Clear();
            NotifyItemsReset();
            return;
        }

        ItemContainerGenerator.OnItemsMoved(oldIndex, newIndex, count);
        MeasuredItemsPanel?.OnItemsMoved(oldIndex, newIndex, count);
        OnItemsMoved(oldIndex, newIndex, count);
    }

    private void NotifyItemsReset()
    {
        MeasuredItemsPanel?.OnItemsReset();
        OnItemsReset();
    }

    private void RebuildItemsSourceSnapshot(IEnumerable? source)
    {
        itemsSourceSnapshot = source is null ? [] : [.. source.Cast<object?>()];
    }

    // Rebuilds the snapshot and returns true when its items or their order changed. A value-type item is boxed
    // again on every read, so it is compared by value; any other item by reference.
    private bool RefreshItemsSourceSnapshot(IEnumerable source)
    {
        IReadOnlyList<object?> previous = itemsSourceSnapshot;
        RebuildItemsSourceSnapshot(source);
        if (previous.Count != itemsSourceSnapshot.Count)
        {
            return true;
        }

        for (int index = 0; index < previous.Count; index++)
        {
            object? oldItem = previous[index];
            object? newItem = itemsSourceSnapshot[index];
            if (!ReferenceEquals(oldItem, newItem) && !(oldItem is ValueType && oldItem.Equals(newItem)))
            {
                return true;
            }
        }

        return false;
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
            container.SetValue(UIElement.AspectProperty, aspect, UiPropertyValueSource.TemplateBinding);
        }
        else
        {
            container.ClearValue(UIElement.AspectProperty, UiPropertyValueSource.TemplateBinding);
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
