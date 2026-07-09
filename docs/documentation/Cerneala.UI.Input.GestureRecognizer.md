# GestureRecognizer Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/GestureRecognizer.cs`

Recognizes tap and drag gestures from a stream of pointer samples.

```csharp
public sealed class GestureRecognizer
```

Inheritance:
`Object` -> `GestureRecognizer`

## Examples

Feed each pointer sample into the recognizer and handle any gesture events it emits.

```csharp
using Cerneala.UI.Input;

GestureRecognizer recognizer = new(dragThreshold: 6);

foreach (GestureEvent gesture in recognizer.Process(new GestureSample(x, y, isPressed)))
{
    if (gesture.Kind == GestureKind.Tap)
    {
        ActivateAt(gesture.X, gesture.Y);
    }
}

static void ActivateAt(float x, float y)
{
    // Invoke the command or selection associated with the tapped location.
}
```

## Remarks

`GestureRecognizer` is stateful. It remembers the sample where the press began, the last pressed sample, and whether the current press sequence has crossed the drag threshold.

When a press begins, `Process` stores the first sample and returns no events. While the pointer remains pressed, movement greater than or equal to the configured threshold emits `DragStarted`; later pressed samples emit `DragDelta` with deltas relative to the previous sample. When the pointer is released after dragging, the recognizer emits `DragCompleted`.

If the pointer is released before movement reaches the threshold, `Process` emits `Tap`.

The constructor rejects negative, infinite, or NaN thresholds.

## Constructors

| Name | Description |
| --- | --- |
| `GestureRecognizer(float)` | Initializes a recognizer with a drag threshold. The default threshold is `4`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(GestureSample)` | `IReadOnlyList<GestureEvent>` | Processes one pointer sample and returns the gesture events produced by that sample. |

## Applies to

- `Cerneala.UI.Input.GestureRecognizer`

## See also

- `Cerneala.UI.Input.GestureSample`
- `Cerneala.UI.Input.GestureEvent`
- `Cerneala.UI.Input.GestureKind`
