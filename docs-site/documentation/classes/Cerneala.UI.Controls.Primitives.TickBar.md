# TickBar Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/TickBar.cs`

Draws the tick marks of a numeric range along its length, as the default `Slider` template shows them beside its track.

```csharp
public class TickBar : UIElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `TickBar`

## Examples

Draw ticks every `2` units below a track whose thumb is `10` pixels long:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

TickBar ticks = new()
{
    Minimum = 0,
    Maximum = 10,
    TickFrequency = 2,
    Placement = TickBarPlacement.Bottom,
    Fill = new SolidColorBrush(new Color(0, 0, 0)),
    ReservedSpace = 10
};

ticks.Measure(new MeasureContext(new LayoutSize(110, 4)));
ticks.Arrange(new ArrangeContext(new LayoutRect(0, 0, 110, 4)));
// Rendering draws 1-pixel lines at x 5 and 105 (4 pixels long, for Minimum and Maximum)
// and at x 25, 45, 65 and 85 (3 pixels long, for the values 2, 4, 6 and 8).
```

Use explicit tick values instead of a frequency:

```csharp
using Cerneala.UI.Controls.Primitives;

TickBar ticks = new()
{
    Minimum = 0,
    Maximum = 10,
    Ticks = [1, 5, 9]
};
```

## Remarks

`TickBar` follows WPF's `TickBar.OnRender`, without its selection ticks. It draws 1-pixel lines of `Fill`; with the default `Fill = null` it draws nothing. Every property is registered with `UiPropertyOptions.AffectsRender`, so a change redraws the element and does not measure or arrange it again. The ticks are drawn only when the element renders; nothing is computed per frame.

`Placement` names the side of the track that the `TickBar` is on, and so the edge its ticks start from. With `TickBarPlacement.Top` the base line is the bottom edge and the ticks grow upward; with `Bottom` the base line is the top edge and they grow downward. With `Left` the base line is the right edge and the ticks grow leftward; with `Right` it is the left edge and they grow rightward.

`ReservedSpace` is the length kept free at the two ends of the `TickBar`, half at each end. A `Slider` sets it to the length of its thumb, so the tick at `Minimum` is under the thumb's center when `Value == Minimum`. When `ReservedSpace` is equal to or larger than the length of the `TickBar`, nothing is drawn.

The ticks at `Minimum` and `Maximum` are always drawn, as long as the `TickBar` is thick. The other ticks are three quarters of that length:

- When `Ticks` has at least one value, a tick is drawn for each value that lies strictly between `Minimum` and `Maximum`, in any order. `TickFrequency` is not used.
- Otherwise, when `TickFrequency` is greater than `0`, a tick is drawn at `Minimum + TickFrequency`, `Minimum + 2 * TickFrequency`, and so on, while the value is below `Maximum`. A `TickFrequency` that would put ticks closer than one pixel is raised to the range of one pixel, so a very small `TickFrequency` draws at most one tick per pixel.

As in WPF, `IsDirectionReversed = false` puts `Minimum` at the left end of a horizontal `TickBar` (`Top` or `Bottom`) and at the bottom end of a vertical one (`Left` or `Right`); `true` puts it at the right end and at the top end.

`TickBar` does not order `Minimum` and `Maximum` and does not coerce anything. `Minimum`, `Maximum`, and every value of `Ticks` must be finite; `TickFrequency` and `ReservedSpace` must be finite and non-negative.

## Constructors

| Name | Description |
| --- | --- |
| `TickBar()` | Initializes a `TickBar` with `Minimum = 0`, `Maximum = 100`, `TickFrequency = 1`, `Placement = TickBarPlacement.Top`, and no `Fill`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `MinimumProperty` | `UiProperty<float>` | Identifies the `Minimum` property. The default value is `0`; values must be finite. |
| `MaximumProperty` | `UiProperty<float>` | Identifies the `Maximum` property. The default value is `100`; values must be finite. |
| `TickFrequencyProperty` | `UiProperty<float>` | Identifies the `TickFrequency` property. The default value is `1`; values must be finite and non-negative. |
| `TicksProperty` | `UiProperty<IReadOnlyList<float>?>` | Identifies the `Ticks` property. The default value is `null`; every value must be finite. |
| `PlacementProperty` | `UiProperty<TickBarPlacement>` | Identifies the `Placement` property. The default value is `TickBarPlacement.Top`; values must be defined `TickBarPlacement` members. |
| `FillProperty` | `UiProperty<Brush?>` | Identifies the `Fill` property. The default value is `null`. |
| `IsDirectionReversedProperty` | `UiProperty<bool>` | Identifies the `IsDirectionReversed` property. The default value is `false`. |
| `ReservedSpaceProperty` | `UiProperty<float>` | Identifies the `ReservedSpace` property. The default value is `0`; values must be finite and non-negative. |

All fields use the metadata option `UiPropertyOptions.AffectsRender`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Minimum` | `float` | Gets or sets the value at the `Minimum` end. |
| `Maximum` | `float` | Gets or sets the value at the `Maximum` end. |
| `TickFrequency` | `float` | Gets or sets the distance between ticks, counted from `Minimum`, used when `Ticks` is `null` or empty. |
| `Ticks` | `IReadOnlyList<float>?` | Gets or sets the tick values. When it has values, they replace `TickFrequency`. |
| `Placement` | `TickBarPlacement` | Gets or sets the side of the track that the `TickBar` is on. |
| `Fill` | `Brush?` | Gets or sets the brush of the ticks. `null` draws nothing. |
| `IsDirectionReversed` | `bool` | Gets or sets whether `Minimum` is at the right end or the top end (`true`) instead of the left end or the bottom end (`false`). |
| `ReservedSpace` | `float` | Gets or sets the length kept free at the two ends, half at each end. |

## Applies to

`Cerneala.UI.Controls.Primitives.TickBar` in the `Cerneala` project.

## See also

- `Cerneala.UI.Controls.Primitives.TickBarPlacement`
- `Cerneala.UI.Controls.Primitives.TickPlacement`
- `Cerneala.UI.Controls.Slider`
- `Cerneala.UI.Controls.Primitives.Track`
