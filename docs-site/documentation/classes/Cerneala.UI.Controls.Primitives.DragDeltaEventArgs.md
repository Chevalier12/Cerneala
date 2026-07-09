# DragDeltaEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/Thumb.cs`

Provides per-update and total drag movement values for the `Thumb.DragDelta` event.

```csharp
public sealed class DragDeltaEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `DragDeltaEventArgs`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;

Thumb thumb = new();

thumb.DragDelta += (_, args) =>
{
    float stepX = args.HorizontalChange;
    float stepY = args.VerticalChange;
    float totalX = args.TotalHorizontalChange;
    float totalY = args.TotalVerticalChange;

    // Apply stepX/stepY to the dragged value or inspect totalX/totalY
    // for movement measured from the start of the drag.
};
```

## Remarks

`DragDeltaEventArgs` is created by `Thumb.UpdateDrag` when a drag is active and the pointer position changes. `HorizontalChange` and `VerticalChange` describe the movement since the previous drag update. `TotalHorizontalChange` and `TotalVerticalChange` describe the movement from the point where the drag started.

`Thumb.DragDelta` is not raised for an update where both per-update changes are zero. The `Track` primitive uses the orientation-specific change, `HorizontalChange` for horizontal tracks and `VerticalChange` for vertical tracks, to update its value.

## Constructors

| Name | Description |
| --- | --- |
| `DragDeltaEventArgs(float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)` | Initializes a new drag delta event argument instance with per-update and total movement values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HorizontalChange` | `float` | Gets the horizontal movement since the previous drag update. |
| `VerticalChange` | `float` | Gets the vertical movement since the previous drag update. |
| `TotalHorizontalChange` | `float` | Gets the horizontal movement from the drag start position. |
| `TotalVerticalChange` | `float` | Gets the vertical movement from the drag start position. |

## Applies to

`Cerneala.UI.Controls.Primitives` in the `Cerneala` project.

## See also

- `Thumb`
- `Thumb.DragDelta`
- `DragStartedEventArgs`
- `DragCompletedEventArgs`
