# DragCompletedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: [UI/Controls/Primitives/Thumb.cs](../../UI/Controls/Primitives/Thumb.cs)

Provides data for the `Thumb.DragCompleted` event.

```csharp
public sealed class DragCompletedEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `DragCompletedEventArgs`

## Examples
```csharp
Thumb thumb = new();

thumb.DragCompleted += (_, args) =>
{
    float horizontal = args.HorizontalChange;
    float vertical = args.VerticalChange;

    if (args.Canceled)
    {
        return;
    }

    // Use horizontal and vertical as the completed drag distance.
};
```

## Remarks
`DragCompletedEventArgs` is raised by `Thumb` when a drag operation ends or is canceled.

When `Thumb.CompleteDrag` finishes a left-button drag, `Canceled` is `false` and `HorizontalChange` / `VerticalChange` contain the thumb's total movement since the drag started. When `Thumb.CancelDrag` runs, including when the thumb is detached while dragging, `Canceled` is `true` and the same total movement fields are reported with the values known at cancellation time.

The constructor stores the values passed to it without additional validation or conversion.

## Constructors
| Name | Description |
| --- | --- |
| `DragCompletedEventArgs(float horizontalChange, float verticalChange, bool canceled)` | Initializes a new instance with the completed horizontal movement, vertical movement, and cancellation flag. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `HorizontalChange` | `float` | Gets the horizontal movement value supplied to the constructor. For `Thumb.DragCompleted`, this is the total horizontal movement since the drag started. |
| `VerticalChange` | `float` | Gets the vertical movement value supplied to the constructor. For `Thumb.DragCompleted`, this is the total vertical movement since the drag started. |
| `Canceled` | `bool` | Gets whether the drag completion represents a canceled drag. |

## Applies to
`Cerneala.UI.Controls.Primitives` in the `Cerneala` project.

## See also
- `Thumb`
- `DragStartedEventArgs`
- `DragDeltaEventArgs`
