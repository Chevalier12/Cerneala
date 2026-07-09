# GestureEvent Struct

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/GestureRecognizer.cs`

Represents a gesture recognized from pointer samples.

```csharp
public readonly record struct GestureEvent(GestureKind Kind, float X, float Y, float DeltaX = 0, float DeltaY = 0)
```

Inheritance:
`ValueType` -> `GestureEvent`

## Examples

Handle the gesture kind and use deltas for drag movement.

```csharp
using Cerneala.UI.Input;

foreach (GestureEvent gesture in recognizer.Process(sample))
{
    switch (gesture.Kind)
    {
        case GestureKind.DragDelta:
            MoveSelectionBy(gesture.DeltaX, gesture.DeltaY);
            break;
        case GestureKind.Tap:
            SelectAt(gesture.X, gesture.Y);
            break;
    }
}
```

## Remarks

`GestureEvent` is an immutable record struct emitted by `GestureRecognizer`. `Kind` identifies the recognized gesture transition. `X` and `Y` store the pointer position for the event.

For `DragStarted`, `DeltaX` and `DeltaY` contain movement from the original press point. For `DragDelta`, they contain movement since the previous sample. `Tap` and `DragCompleted` use the default delta values.

## Constructors

| Name | Description |
| --- | --- |
| `GestureEvent(GestureKind, float, float, float, float)` | Initializes a gesture event with a kind, pointer coordinates, and optional deltas. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DeltaX` | `float` | Gets the horizontal movement associated with the event. |
| `DeltaY` | `float` | Gets the vertical movement associated with the event. |
| `Kind` | `GestureKind` | Gets the recognized gesture kind. |
| `X` | `float` | Gets the event X coordinate. |
| `Y` | `float` | Gets the event Y coordinate. |

## Applies to

- `Cerneala.UI.Input.GestureEvent`

## See also

- `Cerneala.UI.Input.GestureRecognizer`
- `Cerneala.UI.Input.GestureSample`
- `Cerneala.UI.Input.GestureKind`
