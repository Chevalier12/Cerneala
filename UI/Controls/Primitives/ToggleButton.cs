using Cerneala.UI.Core;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls.Primitives;

public class ToggleButton : Button
{
    public ToggleButton()
    {
        Handlers.AddHandler(InputEvents.MouseUpEvent, OnMouseUp);
    }

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

    private void OnMouseUp(UiElementId source, RoutedEventArgs args)
    {
        if (args is MouseButtonEventArgs { ChangedButton: InputMouseButton.Left, ClickCount: > 0 })
        {
            OnToggle();
        }
    }
}
