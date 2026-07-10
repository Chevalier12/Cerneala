using Cerneala.UI.Core;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls.Primitives;

public class ToggleButton : Button
{
    public static readonly RoutedEvent CheckedEvent = RoutedEventRegistry.Register(nameof(Checked), typeof(ToggleButton), RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent UncheckedEvent = RoutedEventRegistry.Register(nameof(Unchecked), typeof(ToggleButton), RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public event RoutedEventHandler Checked { add => AddHandler(CheckedEvent, value); remove => RemoveHandler(CheckedEvent, value); }
    public event RoutedEventHandler Unchecked { add => AddHandler(UncheckedEvent, value); remove => RemoveHandler(UncheckedEvent, value); }

    public static readonly UiProperty<bool> IsCheckedProperty = UiProperty<bool>.Register(
        nameof(IsChecked),
        typeof(ToggleButton),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    protected virtual void OnToggle()
    {
        IsChecked = !IsChecked;
    }

    protected override void OnClick()
    {
        OnToggle();
        RaiseEvent(new RoutedEventArgs(IsChecked ? CheckedEvent : UncheckedEvent, this));
        base.OnClick();
    }
}
