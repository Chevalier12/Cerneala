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

    // The item SelectedIndex referred to after the last selection or item change. An index set
    // before its item exists refers to no item, so item changes leave it unchanged.
    private object? selectedItem;
    private bool hasSelectedItem;
    private bool changingSelectionForItems;
    private bool relocatingSelection;

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
                key.Handlers.AddHandler(InputEvents.MouseUpEvent, created.OnMouseUp);
                return created;
            });
        registration.Owner = this;
    }

    protected internal override void ClearItemContainer(UIElement container)
    {
        base.ClearItemContainer(container);
        if (clickHandlerRegistrations.TryGetValue(container, out SelectorClickHandlerRegistration? registration) &&
            ReferenceEquals(registration.Owner, this))
        {
            registration.Owner = null;
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
        int selectedIndex = selectionModel.SelectedIndex;
        if (hasSelectedItem && selectedIndex >= index)
        {
            if (selectedIndex < index + removedCount)
            {
                ClearSelectionForItems();
            }
            else
            {
                RelocateSelection(selectedIndex - removedCount + addedCount);
            }
        }

        CaptureSelectedItem();
    }

    internal override void OnItemsMoved(int oldIndex, int newIndex, int count)
    {
        if (hasSelectedItem)
        {
            RelocateSelection(GetIndexAfterMove(selectionModel.SelectedIndex, oldIndex, newIndex, count));
        }

        CaptureSelectedItem();
    }

    internal override void OnItemsReset()
    {
        if (hasSelectedItem)
        {
            int selectedIndex = FindItemIndex(selectedItem, selectionModel.SelectedIndex);
            if (selectedIndex < 0)
            {
                ClearSelectionForItems();
            }
            else
            {
                RelocateSelection(selectedIndex);
            }
        }

        CaptureSelectedItem();
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

    // The selected item was removed or replaced.
    private void ClearSelectionForItems()
    {
        changingSelectionForItems = true;
        try
        {
            selectionModel.Clear();
        }
        finally
        {
            changingSelectionForItems = false;
        }
    }

    // The selected item moved to another index: SelectedIndex changes, but the selection does not.
    private void RelocateSelection(int index)
    {
        changingSelectionForItems = true;
        relocatingSelection = true;
        try
        {
            selectionModel.Select(index);
        }
        finally
        {
            changingSelectionForItems = false;
            relocatingSelection = false;
        }
    }

    private void CaptureSelectedItem()
    {
        CaptureSelectedItem(SelectedIndex);
    }

    private void CaptureSelectedItem(int index)
    {
        hasSelectedItem = index >= 0 && index < ItemCount;
        selectedItem = hasSelectedItem ? GetItemAt(index) : null;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        bool forItems = changingSelectionForItems;
        bool relocated = relocatingSelection;
        changingSelectionForItems = false;
        relocatingSelection = false;
        object? removed = selectedItem;
        object? added = args.Change.NewIndex >= 0 && args.Change.NewIndex < ItemCount ? GetItemAt(args.Change.NewIndex) : null;

        // A selection change nested in the SelectedIndex notification below replaces this new item.
        CaptureSelectedItem(args.Change.NewIndex);
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

        RefreshSelectionContainer(args.Change.OldIndex, "Selection changed", ref preparationFailure);
        RefreshSelectionContainer(args.Change.NewIndex, "Selection changed", ref preparationFailure);
        if (!relocated)
        {
            RaiseEvent(new SelectionChangedEventArgs(
                SelectionChangedEvent,
                this,
                args.Change,
                removed is null ? [] : [removed],
                added is null ? [] : [added]));
        }

        ThrowPreparationFailure(preparationFailure);
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

    private sealed class SelectorClickHandlerRegistration
    {
        private readonly UIElement container;

        public SelectorClickHandlerRegistration(UIElement container)
        {
            this.container = container;
        }

        public Selector? Owner { get; set; }

        // As in WPF, where ButtonBase marks the mouse button events handled, a command source in the container, such as
        // a CheckBox from the ItemTemplate, owns its click, as the input bridge resolves the click to it. Any other
        // release is the container's click, and it is handled as WPF's ListBoxItem handles the press, so an enclosing
        // Button or RadioButton is not clicked, checked, or commanded by it (#490).
        public void OnMouseUp(UiElementId _, RoutedEventArgs args)
        {
            if (Owner is null || args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left })
            {
                return;
            }

            UIElement? clickOwner = FindClickOwner(container, args.OriginalSource);
            if (clickOwner is not IInputCommandSource || ReferenceEquals(clickOwner, container))
            {
                Owner.SelectContainer(container);
                args.Handled = true;
            }
        }
    }

    // The element that owns a click whose source is source, walking the visual parents from source up to owner:
    // owner itself, the first IInputCommandSource below owner, or null when source is not in owner's visual subtree.
    internal static UIElement? FindClickOwner(UIElement owner, object? source)
    {
        UIElement? element = source switch
        {
            UIElement candidate => candidate,
            UiElementId id when owner.Root?.ElementIds.TryGetElement(id, out UIElement? resolved) == true => resolved,
            _ => null
        };

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
