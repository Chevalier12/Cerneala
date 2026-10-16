# DragDeltaEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/DragDeltaEventArgs.cs`

Provides per-update and total drag movement values for the `Thumb.DragDelta` event.

```csharp
public sealed class DragDeltaEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `DragDeltaEventArgs`

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

All four values are in the layout space where the thumb is arranged, the space of `Thumb.ArrangedBounds`, not in root pixels: `Thumb` converts the pointer through the render transforms of its ancestors. Adding a change to the thumb's arranged position keeps the drawn thumb under the pointer. Without ancestor transforms the values equal root-pixel movement. Inside a panel with `RenderTransform = new Transform(Matrix3x2.CreateScale(2, 2))`, a pointer move of `14` by `8` root pixels gives `HorizontalChange == 7` and `VerticalChange == 4`.

`Thumb.DragDelta` is not raised for an update where both per-update changes are zero. The `Track` primitive uses the orientation-specific change, `HorizontalChange` for horizontal tracks and `VerticalChange` for vertical tracks, to update its value.

`Thumb` raises these arguments as the bubbling routed event `Thumb.DragDeltaEvent`, with the thumb as `Source`.

## Constructors

| Name | Description |
| --- | --- |
| `DragDeltaEventArgs(float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)` | Initializes a new drag delta event argument instance with per-update and total movement values. `RoutedEvent`, `Source` and `OriginalSource` are not set. |
| `DragDeltaEventArgs(RoutedEvent routedEvent, object source, float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)` | Initializes a new instance for raising `routedEvent` from `source`. Throws `ArgumentNullException` when `routedEvent` or `source` is `null`. `Thumb` uses this constructor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HorizontalChange` | `float` | Gets the horizontal movement since the previous drag update, in the thumb's arrangement layout space. |
| `VerticalChange` | `float` | Gets the vertical movement since the previous drag update, in the thumb's arrangement layout space. |
| `TotalHorizontalChange` | `float` | Gets the horizontal movement from the drag start position, in the thumb's arrangement layout space. |
| `TotalVerticalChange` | `float` | Gets the vertical movement from the drag start position, in the thumb's arrangement layout space. |

`RoutedEvent`, `Source`, `OriginalSource` and `Handled` are inherited from `RoutedEventArgs`.

## Applies to

`Cerneala.UI.Controls.Primitives` in the `Cerneala` project.

## See also

- `Thumb`
- `Thumb.DragDelta`
- `DragStartedEventArgs`
- `DragCompletedEventArgs`
- `Cerneala.UI.Input.RoutedEventArgs`
