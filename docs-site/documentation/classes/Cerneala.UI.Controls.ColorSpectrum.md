# ColorSpectrum Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ColorSpectrum.cs`

Renders and edits the saturation/value plane for an HSV color.

```csharp
public class ColorSpectrum : Control, IPointerDragSource
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ColorSpectrum`

## Examples

```csharp
ColorSpectrum spectrum = new()
{
    Hue = 210
};

spectrum.SetSelection(0.75f, 0.8f);
```

## Remarks

The control paints white-to-hue horizontally and transparent-to-black vertically. Its marker represents the active saturation and value. Pointer click and drag update both channels continuously; arrow keys adjust the focused selection by `0.01`.

`Hue` is coerced to `0..360`. `Saturation` and `Value` are coerced to `0..1`. `SetSelection` updates both channels as one logical operation and raises one `ValueChanged` event.

## Constructors

| Name | Description |
| --- | --- |
| `ColorSpectrum()` | Initializes a focusable spectrum with crosshair pointer feedback. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `HueProperty` | `UiProperty<float>` | Identifies `Hue`; default `0`. |
| `SaturationProperty` | `UiProperty<float>` | Identifies `Saturation`; default `1`. |
| `ValueProperty` | `UiProperty<float>` | Identifies `Value`; default `1`. |
| `ValueChangedEvent` | `RoutedEvent` | Identifies the bubbling value-changed event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Hue` | `float` | Gets or sets hue in degrees. |
| `Saturation` | `float` | Gets or sets horizontal saturation. |
| `Value` | `float` | Gets or sets vertical HSV value. |

## Methods

| Name | Description |
| --- | --- |
| `SetSelection(float saturation, float value)` | Sets saturation and value together and raises one change event when the selection changes. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `ValueChanged` | `RoutedEventHandler` | Occurs after hue, saturation, or value changes. |

## Applies to

`Cerneala.UI.Controls.ColorSpectrum` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.ColorPicker`
- `Cerneala.UI.Controls.ColorSwatch`
