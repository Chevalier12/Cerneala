using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Input;

namespace Cerneala.UI.Controls;

public class RadioButton : Button
{
    public RadioButton()
    {
        Handlers.AddHandler(InputEvents.MouseUpEvent, OnMouseUp);
    }

    public static readonly UiProperty<bool> IsCheckedProperty = UiProperty<bool>.Register(
        nameof(IsChecked),
        typeof(RadioButton),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    private void OnMouseUp(UiElementId source, RoutedEventArgs args)
    {
        if (args is MouseButtonEventArgs { ChangedButton: InputMouseButton.Left })
        {
            IsChecked = true;
        }
    }
}
