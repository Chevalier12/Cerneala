using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Primitives;

public class ButtonBase : UIElement
{
    public static readonly UiProperty<bool> IsPressedProperty = UiProperty<bool>.Register(
        nameof(IsPressed),
        typeof(ButtonBase),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual));

    public bool IsPressed
    {
        get => GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }
}
