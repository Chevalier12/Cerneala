# TickBarPlacement Enum

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/TickBarPlacement.cs`

Specifies the side of a track that a `TickBar` is on, and so the edge its ticks start from.

```csharp
public enum TickBarPlacement
```

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;

TickBar ticks = new() { Placement = TickBarPlacement.Bottom };
// The ticks start at the top edge of the TickBar and grow downward, away from a track above it.
```

## Fields

| Name | Description |
| --- | --- |
| `Left` | The `TickBar` is left of a vertical track. Its base line is its right edge, and the ticks grow leftward. |
| `Top` | The `TickBar` is above a horizontal track. Its base line is its bottom edge, and the ticks grow upward. |
| `Right` | The `TickBar` is right of a vertical track. Its base line is its left edge, and the ticks grow rightward. |
| `Bottom` | The `TickBar` is below a horizontal track. Its base line is its top edge, and the ticks grow downward. |

## Remarks

`Top` and `Bottom` lay the ticks out along the width of the `TickBar`; `Left` and `Right` lay them out along its height. The default `Slider` template uses `Top` and `Bottom` for a horizontal slider and `Left` and `Right` for a vertical one. The values match WPF's `TickBarPlacement`.

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Primitives.TickBar`
- `Cerneala.UI.Controls.Primitives.TickPlacement`
