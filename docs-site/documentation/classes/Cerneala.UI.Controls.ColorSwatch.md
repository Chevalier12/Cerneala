# ColorSwatch Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ColorSwatch.cs`

Displays a compact color sample that opens an adaptive `ColorPicker` overlay when clicked.

```csharp
[TemplatePart("PART_SwatchButton", typeof(Button))]
[TemplatePart("PART_PickerOverlay", typeof(Overlay))]
[TemplatePart("PART_ColorPicker", typeof(ColorPicker))]
public class ColorSwatch : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ColorSwatch`

## Examples

```csharp
ColorSwatch swatch = new()
{
    SelectedColor = new Color(255, 62, 165)
};

swatch.SelectedColorChanged += (_, args) => ApplyColor(args.NewValue);
```

## Remarks

`ColorSwatch` owns the complete compact-picker interaction. Clicking its button opens a light-dismiss overlay placed adaptively around the button. Changes made in the embedded picker update `SelectedColor` immediately, and programmatic color changes synchronize both the displayed swatch and picker.

Use the read-only `Picker` property to style or configure the embedded `ColorPicker`. Setting `IsPickerOpen` opens or closes the same overlay programmatically.

Custom templates must provide every declared template part.

## Constructors

| Name | Description |
| --- | --- |
| `ColorSwatch()` | Initializes a `20` by `20` swatch with its default template. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `SelectedColorProperty` | `UiProperty<Color>` | Identifies `SelectedColor`; default `Color.White`. |
| `IsPickerOpenProperty` | `UiProperty<bool>` | Identifies `IsPickerOpen`; default `false`. |
| `SelectedColorChangedEvent` | `RoutedEvent` | Identifies the bubbling selected-color event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedColor` | `Color` | Gets or sets the displayed and edited color. |
| `IsPickerOpen` | `bool` | Gets or sets whether the picker overlay is open. |
| `Picker` | `ColorPicker` | Applies the template and gets the embedded picker. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `SelectedColorChanged` | `EventHandler<RoutedPropertyChangedEventArgs<Color>>` | Occurs when the selected color changes through code or picker input. |

## Template Parts

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `PART_SwatchButton` | `Button` | Yes | Displays the selected color and opens the picker. |
| `PART_PickerOverlay` | `Overlay` | Yes | Projects the picker with adaptive placement and light dismiss. |
| `PART_ColorPicker` | `ColorPicker` | Yes | Edits the selected color. |

## Applies to

`Cerneala.UI.Controls.ColorSwatch` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.ColorPicker`
- `Cerneala.UI.Controls.ColorSpectrum`
- `Cerneala.UI.Controls.Overlay`
