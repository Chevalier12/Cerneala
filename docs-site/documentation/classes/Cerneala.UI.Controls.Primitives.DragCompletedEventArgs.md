# DragCompletedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: [UI/Controls/Primitives/DragCompletedEventArgs.cs](https://github.com/Chevalier12/Cerneala/blob/master/UI/Controls/Primitives/DragCompletedEventArgs.cs)

Provides data for the `Thumb.DragCompleted` event.

```csharp
public sealed class DragCompletedEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `DragCompletedEventArgs`

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
`DragCompletedEventArgs` is raised by `Thumb` when a drag operation ends or is canceled. `Thumb` raises it as the bubbling routed event `Thumb.DragCompletedEvent`, with the thumb as `Source`, so a parent element can handle it too.

When `Thumb.CompleteDrag` finishes a left-button drag, `Canceled` is `false` and `HorizontalChange` / `VerticalChange` contain the thumb's total movement since the drag started, in the layout space where the thumb is arranged, like `DragDeltaEventArgs.TotalHorizontalChange` and `TotalVerticalChange`; under an ancestor drawn 2x larger, a drag of `30` root pixels reports `15`. When `Thumb.CancelDrag` runs, including when the thumb is detached while dragging, `Canceled` is `true` and the same total movement fields are reported with the values known at cancellation time.

The constructors store the values passed to them without additional validation or conversion.

## Constructors
| Name | Description |
| --- | --- |
| `DragCompletedEventArgs(float horizontalChange, float verticalChange, bool canceled)` | Initializes a new instance with the completed horizontal movement, vertical movement, and cancellation flag. `RoutedEvent`, `Source` and `OriginalSource` are not set. |
| `DragCompletedEventArgs(RoutedEvent routedEvent, object source, float horizontalChange, float verticalChange, bool canceled)` | Initializes a new instance for raising `routedEvent` from `source`. Throws `ArgumentNullException` when `routedEvent` or `source` is `null`. `Thumb` uses this constructor. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `HorizontalChange` | `float` | Gets the horizontal movement value supplied to the constructor. For `Thumb.DragCompleted`, this is the total horizontal movement since the drag started. |
| `VerticalChange` | `float` | Gets the vertical movement value supplied to the constructor. For `Thumb.DragCompleted`, this is the total vertical movement since the drag started. |
| `Canceled` | `bool` | Gets whether the drag completion represents a canceled drag. |

`RoutedEvent`, `Source`, `OriginalSource` and `Handled` are inherited from `RoutedEventArgs`.

## Applies to
`Cerneala.UI.Controls.Primitives` in the `Cerneala` project.

## See also
- `Thumb`
- `DragStartedEventArgs`
- `DragDeltaEventArgs`
- `Cerneala.UI.Input.RoutedEventArgs`
