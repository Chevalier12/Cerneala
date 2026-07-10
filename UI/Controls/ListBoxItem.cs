using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Input;
using Cerneala.UI.Controls.Primitives;

namespace Cerneala.UI.Controls;

public class ListBoxItem : ContentControl, ISelectableItemContainer
{
    public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(ListBoxItem));
    public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(ListBoxItem));

    public event RoutedEventHandler Selected { add => AddHandler(SelectedEvent, value); remove => RemoveHandler(SelectedEvent, value); }
    public event RoutedEventHandler Unselected { add => AddHandler(UnselectedEvent, value); remove => RemoveHandler(UnselectedEvent, value); }
    public static readonly UiProperty<bool> IsSelectedProperty = UiProperty<bool>.Register(
        nameof(IsSelected),
        typeof(ListBoxItem),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect));

    public int ItemIndex { get; set; } = -1;

    public object? Item { get; set; }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, IsSelectedProperty))
        {
            RaiseEvent(new RoutedEventArgs(IsSelected ? SelectedEvent : UnselectedEvent, this));
        }
    }

    protected override void OnRender(RenderContext context)
    {
        DrawColor color = IsSelected ? new DrawColor(80, 130, 220) : Background;
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (color.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, color);
        }
    }
}
