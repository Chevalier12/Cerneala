# ProgressBar Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ProgressBar.cs`

Displays a horizontal retained progress indicator whose fill is derived from the inherited range value.

```csharp
public class ProgressBar : RangeBase
```

Inheritance:
`Object` -> `UiObject` -> `UIElement` -> `Control` -> `RangeBase` -> `ProgressBar`

## Examples

Create a progress bar that is 25 percent filled:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

ProgressBar progress = new()
{
    Minimum = 0,
    Maximum = 100,
    Value = 25,
    Foreground = new SolidColorBrush(Color.White)
};

float ratio = progress.ValueRatio; // 0.25f
```

## Remarks

`ProgressBar` is a retained UI control built on `RangeBase`. It uses the inherited `Minimum`, `Maximum`, and `Value` properties to compute `ValueRatio`, then renders the foreground fill across that fraction of its arranged width.

The constructor assigns visual defaults for the progress track: `Background` is `new SolidColorBrush(new Color(230, 230, 230))`, `Foreground` is `Color(65, 135, 230)`, `BorderBrush` is `new SolidColorBrush(new Color(120, 120, 120))`, and `BorderThickness` is `new Thickness(1)`.

The control measures to `100 x 12` before margin and parent layout constraints are applied. Rendering fills the background first, fills the progress foreground second, and draws the border last. The rendered fill width clamps `ValueRatio` to the `0..1` range before multiplying by the arranged width.

`RangeBase` coerces `Value` into the current `Minimum..Maximum` range. If `Minimum` and `Maximum` collapse to the same value, `ValueRatio` returns `0`.

## Constructors

| Name | Description |
| --- | --- |
| `ProgressBar()` | Initializes a new `ProgressBar` with default track, fill, border color, and border thickness values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ValueRatio` | `float` | Gets `(Value - Minimum) / (Maximum - Minimum)`, or `0` when `Maximum <= Minimum`. Rendering clamps this ratio before drawing the fill. |

## Relevant Inherited Fields

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Minimum` property. Default is `0`; values must be finite. Changes affect arrange and render. |
| `MaximumProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Maximum` property. Default is `1`; values must be finite. Changes affect arrange and render. |
| `ValueProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `Value` property. Default is `0`; values must be finite and are coerced to the active range. Changes affect arrange, render, and input visual state. |
| `SmallChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `SmallChange` property. Default is `0.1`; values must be finite and non-negative. |
| `LargeChangeProperty` | `UiProperty<float>` | `RangeBase` | Identifies the `LargeChange` property. Default is `1`; values must be finite and non-negative. |

## Relevant Inherited Properties

| Name | Type | Declared By | Description |
| --- | --- | --- | --- |
| `Minimum` | `float` | `RangeBase` | Gets or sets the lower bound for `Value`. If set above `Maximum`, `Maximum` is raised to match it. |
| `Maximum` | `float` | `RangeBase` | Gets or sets the upper bound for `Value`. If set below `Minimum`, `Minimum` is lowered to match it. |
| `Value` | `float` | `RangeBase` | Gets or sets the current progress value. The value is coerced into the current range. |
| `SmallChange` | `float` | `RangeBase` | Gets or sets the small range increment. It is inherited from `RangeBase`; `ProgressBar` does not use it during rendering. |
| `LargeChange` | `float` | `RangeBase` | Gets or sets the large range increment. It is inherited from `RangeBase`; `ProgressBar` does not use it during rendering. |
| `Background` | `Brush?` | `Control` | Gets or sets the track fill brush. The `ProgressBar` constructor sets a solid brush with color `Color(230, 230, 230)`. |
| `Foreground` | `Color` | `Control` | Gets or sets the progress fill color. The `ProgressBar` constructor sets it to `Color(65, 135, 230)`. |
| `BorderBrush` | `Brush?` | `Control` | Gets or sets the outline brush. The constructor sets a solid brush with color `Color(120, 120, 120)`. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the source thickness for the rendered outline. The `ProgressBar` constructor sets it to `new Thickness(1)`. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Background != null` and arranged width/height are positive | Emits a filled rectangle for the full progress bar track. |
| `Foreground.A != 0`, clamped fill width is positive, and arranged height is positive | Emits a filled rectangle from the left edge whose width is `arranged width * Clamp(ValueRatio, 0, 1)`. |
| `BorderBrush != null`, effective border thickness is positive, and arranged width/height are positive | Emits a rectangle stroke around the arranged bounds. |

## Layout Behavior

| Behavior | Value |
| --- | --- |
| Desired size from `MeasureCore` | `new LayoutSize(100, 12)` |
| Fill direction | Left to right across the arranged width |
| Effective border stroke thickness | Maximum of `BorderThickness.Left`, `Top`, `Right`, and `Bottom` |

## Applies To

Cerneala retained UI controls and layout/rendering infrastructure.

## See Also

- `Cerneala.UI.Controls.Primitives.RangeBase`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Layout.Thickness`
- `Cerneala.Drawing.Color`
