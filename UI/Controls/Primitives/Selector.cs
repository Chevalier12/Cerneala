using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Cerneala.UI.Core;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Controls.Selection;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls.Primitives;

public class Selector : ItemsControl
{
    private static readonly ConditionalWeakTable<UIElement, SelectorClickHandlerRegistration> clickHandlerRegistrations = new();
    private readonly SelectionModel selectionModel = new();

    // The item each selected index referred to after the last selection or item change, in selection order. An index
    // set before its item exists refers to no item, so item changes leave it unchanged.
    private readonly List<SelectedEntry> selectedEntries = [];

    // Set by an item change just before it moves the selection: the selected items that it removed. That change
    // deselects them and raises SelectionChanged with them; a change that only moved selected items raises none.
    private List<object?>? itemsChangeRemovedItems;

    // The number of selection changes notifying their new SelectedIndex. A selection change nested in that
    // notification, or a subclass refreshing its own containers in it, hands its first re-preparation failure to the
    // outermost change, which rethrows it only after its own refresh and SelectionChanged.
    private int selectedIndexNotificationDepth;
    private ExceptionDispatchInfo? nestedPreparationFailure;

    // A SelectedIndex change made through SetValue, a binding, or an aspect, which the selection change that follows
    // it notifies.
    private UiPropertyChange? pendingSelectedIndexChange;

    public Selector()
    {
        selectionModel.SelectionChanged += OnSelectionChanged;
    }

    public static readonly RoutedEvent SelectionChangedEvent = RoutedEventRegistry.Register(nameof(SelectionChanged), typeof(Selector), RoutingStrategy.Bubble, typeof(SelectionChangedEventArgs));
    public static readonly RoutedEvent SelectedEvent = RoutedEventRegistry.Register("Selected", typeof(Selector), RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent UnselectedEvent = RoutedEventRegistry.Register("Unselected", typeof(Selector), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public event EventHandler<SelectionChangedEventArgs> SelectionChanged
    {
        add => AddTypedHandler(SelectionChangedEvent, value);
        remove => RemoveTypedHandler(SelectionChangedEvent, value);
    }

    public static readonly UiProperty<int> SelectedIndexProperty = UiProperty<int>.Register(
        nameof(SelectedIndex),
        typeof(Selector),
        new UiPropertyMetadata<int>(-1, UiPropertyOptions.None, validateValue: value => value >= -1));

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => selectionModel.Select(value);
    }

    public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < ItemCount ? GetItemAt(SelectedIndex) : null;

    public SelectionModel SelectionModel => selectionModel;

    // True while SelectedIndex changes because the items changed, not because a selection was made.
    internal bool IsChangingSelectionForItems { get; private set; }

    protected internal override bool IsItemSelected(int index)
    {
        return selectionModel.IsSelected(index);
    }

    protected internal override void PrepareItemContainer(UIElement container, int index, object? item)
    {
        base.PrepareItemContainer(container, index, item);
        SelectorClickHandlerRegistration registration = clickHandlerRegistrations.GetValue(
            container,
            static key =>
            {
                SelectorClickHandlerRegistration created = new(key);
                key.Handlers.AddHandler(InputEvents.MouseDownEvent, created.OnMouseDown, handledEventsToo: true);
                key.Handlers.AddHandler(InputEvents.MouseUpEvent, created.OnMouseUp);
                return created;
            });
        registration.Owner = this;
        container.IsSelectorItemContainer = true;
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        base.ClearItemContainer(container);
        if (clickHandlerRegistrations.TryGetValue(container, out SelectorClickHandlerRegistration? registration) &&
            ReferenceEquals(registration.Owner, this))
        {
            registration.Owner = null;
            container.IsSelectorItemContainer = false;
        }
    }

    protected virtual void SelectContainer(UIElement container)
    {
        int index = ItemContainerGenerator.IndexFromContainer(container);
        if (index >= 0)
        {
            SelectedIndex = index;
        }
    }

    // Selects the container for a left click that it takes. A subclass that selects by the modifier keys of the click,
    // such as a ListBox whose SelectionMode is not Single, overrides it.
    internal virtual void SelectContainerOnClick(UIElement container, MouseButtonEventArgs args)
    {
        SelectContainer(container);
    }

    // Whether a left click that the container takes selects it. Every click does, unless a subclass requires more. It is
    // asked at the left press, before the press moves the focus, and the release of that press uses that answer; a
    // release without a press on the container asks at the release.
    internal virtual bool SelectsContainerOnClick(UIElement container, MouseButtonEventArgs args) => true;

    // SetValue, a binding, or an aspect changed SelectedIndex without a selection. The selection model follows first,
    // and that selection change notifies the new SelectedIndex in place of its own SetValue. So the change behaves like
    // the SelectedIndex setter: every PropertyChanged handler and override runs inside the selection change, which
    // then re-prepares its containers, raises SelectionChanged, and only then rethrows a failed re-preparation.
    internal override void NotifyPropertyChange(in UiPropertyChange change)
    {
        if (!ReferenceEquals(change.Args.Property, SelectedIndexProperty) || selectionModel.SelectedIndex == SelectedIndex)
        {
            base.NotifyPropertyChange(change);
            return;
        }

        pendingSelectedIndexChange = change;
        selectionModel.Select(SelectedIndex);
    }

    internal override void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        UpdateSelectionForItems(entry => entry.Index < index
            ? entry.Index
            : entry.Index < index + removedCount ? -1 : entry.Index - removedCount + addedCount);
    }

    internal override void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
        UpdateSelectionForItems(entry => GetIndexAfterMove(entry.Index, oldIndex, newIndex, count));
    }

    internal override void OnItemsReset()
    {
        UpdateSelectionForItems(entry => FindItemIndex(entry.Item, entry.Index));
    }

    // The index of the item at itemIndex after count items moved from oldIndex to newIndex.
    internal static int GetIndexAfterMove(int itemIndex, int oldIndex, int newIndex, int count)
    {
        if (itemIndex >= oldIndex && itemIndex < oldIndex + count)
        {
            return newIndex + itemIndex - oldIndex;
        }

        itemIndex -= itemIndex >= oldIndex + count ? count : 0;
        itemIndex += itemIndex >= newIndex ? count : 0;
        return itemIndex;
    }

    // The index of item after a reset, checking its index before the reset first, or -1 when it is gone.
    internal int FindItemIndex(object? item, int previousIndex)
    {
        if (previousIndex >= 0 && previousIndex < ItemCount && Equals(GetItemAt(previousIndex), item))
        {
            return previousIndex;
        }

        for (int index = 0; index < ItemCount; index++)
        {
            if (Equals(GetItemAt(index), item))
            {
                return index;
            }
        }

        return -1;
    }

    // Every selected item follows its item to the index that newIndexOf gives, or is deselected when it gives -1, as
    // the item was removed or replaced. Selected items that only moved change SelectedIndex, not the selection, so they
    // raise no SelectionChanged.
    private void UpdateSelectionForItems(Func<SelectedEntry, int> newIndexOf)
    {
        List<int> indices = new(selectedEntries.Count);
        HashSet<int> kept = [];
        List<object?> removed = [];
        foreach (SelectedEntry entry in selectedEntries)
        {
            int index = entry.HasItem ? newIndexOf(entry) : entry.Index;
            if (index < 0)
            {
                removed.Add(entry.Item);
            }
            else if (kept.Add(index))
            {
                indices.Add(index);
            }
        }

        if (removed.Count > 0 || !indices.SequenceEqual(selectionModel.SelectedIndices))
        {
            itemsChangeRemovedItems = removed;
            try
            {
                selectionModel.SetSelectedIndices(indices);
            }
            finally
            {
                itemsChangeRemovedItems = null;
            }
        }

        CaptureSelectedItems();
    }

    private void CaptureSelectedItems()
    {
        selectedEntries.Clear();
        foreach (int index in selectionModel.SelectedIndices)
        {
            bool hasItem = index < ItemCount;
            selectedEntries.Add(new SelectedEntry(index, hasItem ? GetItemAt(index) : null, hasItem));
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        List<object?>? removedForItems = itemsChangeRemovedItems;
        itemsChangeRemovedItems = null;
        bool forItems = removedForItems is not null;
        bool relocated = removedForItems is { Count: 0 };
        List<object?> removed = [];
        List<object?> added = [];
        if (removedForItems is not null)
        {
            removed.AddRange(removedForItems.Where(item => item is not null));
        }
        else
        {
            HashSet<int> removedIndices = [.. args.RemovedIndices];
            foreach (SelectedEntry entry in selectedEntries)
            {
                if (entry.HasItem && entry.Item is not null && removedIndices.Contains(entry.Index))
                {
                    removed.Add(entry.Item);
                }
            }

            foreach (int index in args.AddedIndices)
            {
                if (index < ItemCount && GetItemAt(index) is { } item)
                {
                    added.Add(item);
                }
            }
        }

        // A selection change nested in the SelectedIndex notification below replaces these new items.
        CaptureSelectedItems();
        bool wasChangingSelectionForItems = IsChangingSelectionForItems;
        IsChangingSelectionForItems = forItems;
        ExceptionDispatchInfo? preparationFailure = null;
        selectedIndexNotificationDepth++;
        try
        {
            if (pendingSelectedIndexChange is { } change)
            {
                pendingSelectedIndexChange = null;
                RaisePropertyChange(change);
            }
            else
            {
                SetValue(SelectedIndexProperty, args.Change.NewIndex);
            }
        }
        finally
        {
            IsChangingSelectionForItems = wasChangingSelectionForItems;
            if (--selectedIndexNotificationDepth == 0)
            {
                preparationFailure = nestedPreparationFailure;
                nestedPreparationFailure = null;
            }
        }

        try
        {
            foreach (int index in args.RemovedIndices)
            {
                RefreshSelectionContainer(index, "Selection changed", ref preparationFailure);
            }

            foreach (int index in args.AddedIndices)
            {
                RefreshSelectionContainer(index, "Selection changed", ref preparationFailure);
            }

            if (!relocated)
            {
                RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, this, args.Change, removed, added));
            }
        }
        finally
        {
            OnSelectionChangeCompleted();
        }

        ThrowPreparationFailure(preparationFailure);
    }

    // Runs at the end of every selection change, after the Selected, Unselected, and SelectionChanged handlers, also
    // when one of them throws. As in WPF, where a subclass's OnSelectionChanged override runs after
    // base.OnSelectionChanged raised SelectionChanged, a subclass can react here to what those handlers changed.
    internal virtual void OnSelectionChangeCompleted()
    {
    }

    // A re-preparation failure raised while a selection change notifies its new SelectedIndex, by a nested
    // selection change or by a subclass that refreshes its own containers in that notification, goes to the
    // outermost change, which rethrows it only after its own refresh and SelectionChanged. Outside that
    // notification it is rethrown at once.
    internal void ThrowPreparationFailure(ExceptionDispatchInfo? preparationFailure)
    {
        if (selectedIndexNotificationDepth > 0)
        {
            nestedPreparationFailure ??= preparationFailure;
            return;
        }

        preparationFailure?.Throw();
    }

    // The selection state already holds the new index, so a container whose re-preparation throws does not stop
    // the rest of the selection change, such as the other container's refresh or the SelectionChanged event. The
    // caller rethrows the first failure after them.
    internal void RefreshSelectionContainer(int index, string reason, ref ExceptionDispatchInfo? preparationFailure)
    {
        try
        {
            InvalidateContainer(index, reason);
        }
        catch (Exception exception)
        {
            preparationFailure ??= ExceptionDispatchInfo.Capture(exception);
        }
    }

    // A realized container whose item was replaced keeps its index until the next layout pass prepares it for
    // the new item, so a container is refreshed here only while it holds the item at its index. ItemCollection
    // stores an equal item without a change, so a container holding an equal item still holds its item.
    // A preparation that throws leaves the container recorded as unprepared and the items dirty, so the next
    // layout pass prepares it again.
    internal void InvalidateContainer(int index, string reason)
    {
        if (index < 0 || index >= ItemCount || !ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? container))
        {
            return;
        }

        object? item = GetItemAt(index);
        if (!Equals(ItemContainerGenerator.GetItem(container), item))
        {
            return;
        }

        try
        {
            ItemContainerGenerator.PrepareRealizedContainer(container, index, item);
        }
        catch
        {
            ItemsPresenter.MarkItemsDirty();
            InvalidateItems(reason);
            throw;
        }

        container.IncrementRenderVersion();
        container.Invalidate(InvalidationFlags.Render | InvalidationFlags.InputVisual, reason);
    }

    private readonly record struct SelectedEntry(int Index, object? Item, bool HasItem);

    private sealed class SelectorClickHandlerRegistration
    {
        private readonly UIElement container;

        public SelectorClickHandlerRegistration(UIElement container)
        {
            this.container = container;
        }

        public Selector? Owner { get; set; }

        // The last left press on the container: its owner, its original source, and whether that owner selects the
        // container on its click. As WPF's ListBoxItem decides at the press, when its Focus() succeeds, a handler that
        // the press's focus change runs, such as a GotFocus handler that sets Focusable to false, does not change it.
        private Selector? pressOwner;
        private object? pressSource;
        private bool pressSelects;

        public void OnMouseDown(object _, RoutedEventArgs args)
        {
            if (Owner is null || args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left } mouseArgs)
            {
                return;
            }

            pressOwner = Owner;
            pressSource = args.OriginalSource;
            pressSelects = Owner.SelectsContainerOnClick(container, mouseArgs);
        }

        // As in WPF, where ButtonBase marks the mouse button events handled, a command source in the container, such as
        // a CheckBox from the ItemTemplate, owns its click, as the input bridge resolves the click to it. A release that
        // completes no click, because the press was elsewhere in the container, is not the command source's click, so
        // it still selects the container, as the press on the row does in WPF, unless the Selector does not select
        // the container on a click (SelectsContainerOnClick). Any release that the container takes is handled, also
        // one that does not select it, as WPF's ListBoxItem handles the press, so an enclosing Button or RadioButton is
        // not clicked, checked, or commanded by it (#490). The click owner is found with the route map the input
        // bridge routed the MouseUp with, so it stays the same when an earlier handler, such as the CheckBox's Click,
        // removes the clicked element from the container or detaches the Selector (#550).
        public void OnMouseUp(object _, RoutedEventArgs args)
        {
            if (Owner is null || args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left } mouseArgs)
            {
                return;
            }

            bool releasesPress = ReferenceEquals(pressOwner, Owner) &&
                pressSource is not null &&
                ReferenceEquals(mouseArgs.PressOriginalSource, pressSource);
            bool pressSelectsContainer = pressSelects;
            pressOwner = null;
            pressSource = null;
            UIElement? clickOwner = FindClickOwner(container, args.OriginalSource, mouseArgs.RouteMap);
            if (mouseArgs.ClickCount <= 0 || clickOwner is not IInputCommandSource || ReferenceEquals(clickOwner, container))
            {
                if (releasesPress ? pressSelectsContainer : Owner.SelectsContainerOnClick(container, mouseArgs))
                {
                    Owner.SelectContainerOnClick(container, mouseArgs);
                }

                args.Handled = true;
            }
        }
    }

    // The element that owns a click whose source is source, walking the visual parents from source up to owner:
    // owner itself, the first IInputCommandSource below owner, or null when source is not in owner's visual subtree.
    // With routeMap, the route map an input event was routed with, source and its visual parents are those of that
    // route map, not of the live tree, which a handler that ran earlier on the route may have changed.
    internal static UIElement? FindClickOwner(UIElement owner, object? source, ElementInputRouteMap? routeMap = null)
    {
        if (routeMap is not null)
        {
            UIElement? routed = source as UIElement;

            if (routed is null)
            {
                return null;
            }

            foreach (UIElement current in routeMap.GetVisualRouteToRoot(routed))
            {
                if (ReferenceEquals(current, owner) || current is IInputCommandSource)
                {
                    return current;
                }
            }

            return null;
        }

        UIElement? element = source as UIElement;

        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (ReferenceEquals(current, owner) || current is IInputCommandSource)
            {
                return current;
            }
        }

        return null;
    }
}
