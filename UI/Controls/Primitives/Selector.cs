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

    public Selector()
    {
        selectionModel.SelectionChanged += OnSelectionChanged;
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

    public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

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

    protected void SelectContainer(UIElement container)
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

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        SetValue(SelectedIndexProperty, args.Change.NewIndex);
        InvalidateContainer(args.Change.OldIndex);
        InvalidateContainer(args.Change.NewIndex);
    }

    private void InvalidateContainer(int index)
    {
        if (index < 0 || !ItemContainerGenerator.RealizedContainers.TryGetValue(index, out UIElement? container))
        {
            return;
        }

        PrepareItemContainer(container, index, Items[index]);
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
