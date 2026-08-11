# ColorPicker Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ColorPicker.cs`

Provides a templated HSV color editor with saturation/value, hue, alpha, and preview controls.

```csharp
[TemplatePart("PART_Spectrum", typeof(ColorSpectrum))]
[TemplatePart("PART_HueSlider", typeof(Slider))]
[TemplatePart("PART_AlphaSlider", typeof(Slider))]
[TemplatePart("PART_PreviewSwatch", typeof(Border))]
public class ColorPicker : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ColorPicker`

## Examples

```csharp
ColorPicker picker = new()
{
    SelectedColor = new Color(32, 128, 240, 192)
};

picker.SelectedColorChanged += (_, args) =>
{
    Color selected = args.NewValue;
};
```

## Remarks

The default template combines a two-dimensional `ColorSpectrum`, a hue slider, an alpha slider, and a preview swatch. Input from any template part updates `SelectedColor` immediately. Assigning `SelectedColor` synchronizes every part and exposes the converted HSV and alpha channels through read-only properties.

Hue is expressed in degrees from `0` through `360`. Saturation, value, and alpha use the `0..1` range. The alpha slider changes only the alpha channel, while hue and spectrum changes preserve the current alpha. Its ramp composites the transparent-to-opaque color gradient over a checkerboard so transparency remains visually distinguishable on any picker background.

Custom templates must provide every declared template part.

## Constructors

| Name | Description |
| --- | --- |
| `ColorPicker()` | Initializes the picker with a white selected color and its default template. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `SelectedColorProperty` | `UiProperty<Color>` | Identifies `SelectedColor`; the default is `Color.White`. |
| `SelectedColorChangedEvent` | `RoutedEvent` | Identifies the bubbling selected-color event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedColor` | `Color` | Gets or sets the currently edited color. |
| `Hue` | `float` | Gets the current hue in degrees. |
| `Saturation` | `float` | Gets the current saturation in the `0..1` range. |
| `Value` | `float` | Gets the current HSV value in the `0..1` range. |
| `Alpha` | `float` | Gets the current alpha in the `0..1` range. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `SelectedColorChanged` | `EventHandler<RoutedPropertyChangedEventArgs<Color>>` | Occurs when `SelectedColor` changes. |

## Template Parts

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `PART_Spectrum` | `ColorSpectrum` | Yes | Edits saturation and value. |
| `PART_HueSlider` | `Slider` | Yes | Edits hue from `0` through `360`. |
| `PART_AlphaSlider` | `Slider` | Yes | Edits alpha from `0` through `1`. |
| `PART_PreviewSwatch` | `Border` | Yes | Displays the selected color. |

## Applies to

`Cerneala.UI.Controls.ColorPicker` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.ColorSpectrum`
- `Cerneala.UI.Controls.ColorSwatch`
- `Cerneala.UI.Controls.Slider`
