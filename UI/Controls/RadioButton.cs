using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

public class RadioButton : Button
{
    public static readonly UiProperty<bool> IsCheckedProperty = UiProperty<bool>.Register(
        nameof(IsChecked),
        typeof(RadioButton),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsAspect));

    // WPF's RadioButton style is not based on the ButtonBase style, so its content keeps Control's Left and Top.
    public RadioButton()
    {
        SetFrameworkDefault(HorizontalContentAlignmentProperty, HorizontalAlignment.Left);
        SetFrameworkDefault(VerticalContentAlignmentProperty, VerticalAlignment.Top);
    }

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    // Checks only through ButtonBase's click, so a release that a command source in the content owns, or a release
    // that follows no press on this button, leaves IsChecked unchanged (#587).
    protected override void OnClick()
    {
        IsChecked = true;
        base.OnClick();
    }
}
