# TickPlacement Enum

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/TickPlacement.cs`

Specifies where a `Slider` shows its tick marks.

```csharp
public enum TickPlacement
```

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

Slider slider = new()
{
    Minimum = 0,
    Maximum = 10,
    TickFrequency = 2,
    TickPlacement = TickPlacement.BottomRight
};
// Ticks at 0, 2, 4, 6, 8 and 10 are drawn below the track.
```

## Fields

| Name | Description |
| --- | --- |
| `None` | No ticks are drawn. This is the default of `Slider.TickPlacement`. |
| `TopLeft` | Ticks are drawn above a horizontal slider and left of a vertical one. |
| `BottomRight` | Ticks are drawn below a horizontal slider and right of a vertical one. |
| `Both` | Ticks are drawn on both sides of the track. |

## Remarks

In the default `Slider` template, each side has a `TickBar` that is 4 pixels thick and 2 pixels away from the track. A side without ticks has its `TickBar` collapsed, so it takes no space: a horizontal slider with `TickPlacement.Both` is 12 pixels taller than the same slider with `TickPlacement.None`. The values match WPF's `TickPlacement`.

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Slider`
- `Cerneala.UI.Controls.Primitives.TickBar`
- `Cerneala.UI.Controls.Primitives.TickBarPlacement`
