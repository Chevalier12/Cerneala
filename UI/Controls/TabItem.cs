using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

public class TabItem : ContentControl, ISelectableItemContainer
{
    public static readonly UiProperty<bool> IsSelectedProperty = UiProperty<bool>.Register(
        nameof(IsSelected),
        typeof(TabItem),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsStyle));

    public static readonly UiProperty<object?> HeaderProperty = UiProperty<object?>.Register(
        nameof(Header),
        typeof(TabItem),
        new UiPropertyMetadata<object?>(null, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public int ItemIndex { get; set; } = -1;

    public object? Item { get; set; }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}
