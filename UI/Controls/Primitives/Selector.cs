using System.Runtime.CompilerServices;
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
    private bool relocatingSelection;

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
        int index = global::Cerneala.UI.Controls.Items.ItemContainerGenerator.GetItemIndex(container);
        if (index >= 0)
        {
            SelectedIndex = index;
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, SelectedIndexProperty) && selectionModel.SelectedIndex != SelectedIndex)
        {
            selectionModel.Select(SelectedIndex);
        }
    }

    internal override void OnItemsReplaced(int index, int removedCount, int addedCount)
    {
        int selectedIndex = selectionModel.SelectedIndex;
        if (hasSelectedItem && selectedIndex >= index)
        {
            if (selectedIndex < index + removedCount)
            {
                selectionModel.Clear();
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
            int selectedIndex = selectionModel.SelectedIndex;
            if (selectedIndex >= oldIndex && selectedIndex < oldIndex + count)
            {
                selectedIndex = newIndex + selectedIndex - oldIndex;
            }
            else
            {
                selectedIndex -= selectedIndex >= oldIndex + count ? count : 0;
                selectedIndex += selectedIndex >= newIndex ? count : 0;
            }

            RelocateSelection(selectedIndex);
        }

        CaptureSelectedItem();
    }

    internal override void OnItemsReset()
    {
        if (hasSelectedItem)
        {
            int selectedIndex = FindSelectedItem();
            if (selectedIndex < 0)
            {
                selectionModel.Clear();
            }
            else
            {
                RelocateSelection(selectedIndex);
            }
        }

        CaptureSelectedItem();
    }

    private int FindSelectedItem()
    {
        int selectedIndex = selectionModel.SelectedIndex;
        if (selectedIndex < ItemCount && Equals(GetItemAt(selectedIndex), selectedItem))
        {
            return selectedIndex;
        }

        for (int index = 0; index < ItemCount; index++)
        {
            if (Equals(GetItemAt(index), selectedItem))
            {
                return index;
            }
        }

        return -1;
    }

    // The selected item moved to another index: SelectedIndex changes, but the selection does not.
    private void RelocateSelection(int index)
    {
        relocatingSelection = true;
        try
        {
            selectionModel.Select(index);
        }
        finally
        {
            relocatingSelection = false;
        }
    }

    private void CaptureSelectedItem()
    {
        hasSelectedItem = SelectedIndex >= 0 && SelectedIndex < ItemCount;
        selectedItem = hasSelectedItem ? GetItemAt(SelectedIndex) : null;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        bool relocated = relocatingSelection;
        relocatingSelection = false;
        object? removed = selectedItem;
        object? added = args.Change.NewIndex >= 0 && args.Change.NewIndex < ItemCount ? GetItemAt(args.Change.NewIndex) : null;
        SetValue(SelectedIndexProperty, args.Change.NewIndex);
        CaptureSelectedItem();
        InvalidateContainer(args.Change.OldIndex);
        InvalidateContainer(args.Change.NewIndex);
        if (relocated)
        {
            return;
        }

        RaiseEvent(new SelectionChangedEventArgs(
            SelectionChangedEvent,
            this,
            args.Change,
            removed is null ? [] : [removed],
            added is null ? [] : [added]));
    }

    // Realized containers keep their old indexes until the next layout pass, so an index past the current items has no item to prepare.
    private void InvalidateContainer(int index)
    {
        if (index < 0 || index >= ItemCount || !ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? container))
        {
            return;
        }

        PrepareItemContainer(container, index, GetItemAt(index));
        container.IncrementRenderVersion();
        container.Invalidate(InvalidationFlags.Render | InvalidationFlags.InputVisual, "Selection changed");
    }

    private sealed class SelectorClickHandlerRegistration
    {
        private readonly UIElement container;

        public SelectorClickHandlerRegistration(UIElement container)
        {
            this.container = container;
        }

        public Selector? Owner { get; set; }

        public void OnMouseUp(UiElementId _, RoutedEventArgs args)
        {
            if (Owner is not null && args is MouseButtonEventArgs { ChangedButton: InputMouseButton.Left })
            {
                Owner.SelectContainer(container);
            }
        }
    }
}
