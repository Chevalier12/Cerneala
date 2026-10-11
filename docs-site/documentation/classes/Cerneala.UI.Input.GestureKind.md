# GestureKind Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/GestureRecognizer.cs`

Identifies the kind of gesture that `GestureRecognizer` reports.

```csharp
public enum GestureKind
```

## Examples

Feed pressed and released samples to a recognizer with the default 4-unit drag threshold:

```csharp
using Cerneala.UI.Input;

GestureRecognizer recognizer = new();

recognizer.Process(new GestureSample(10, 10, IsPressed: true));   // no events
recognizer.Process(new GestureSample(11, 10, IsPressed: false));  // Tap at (11, 10)

recognizer.Process(new GestureSample(10, 10, IsPressed: true));   // no events
recognizer.Process(new GestureSample(20, 10, IsPressed: true));   // DragStarted, DeltaX = 10 (from the press)
recognizer.Process(new GestureSample(25, 10, IsPressed: true));   // DragDelta, DeltaX = 5 (from the last sample)
IReadOnlyList<GestureEvent> events =
    recognizer.Process(new GestureSample(25, 10, IsPressed: false));

Console.WriteLine(events[0].Kind); // DragCompleted
```

## Remarks

`GestureRecognizer.Process` returns a list of `GestureEvent` values. Each event has a `Kind`, the sample position `X` and `Y`, and `DeltaX` and `DeltaY`.

| Kind | When it is reported | `DeltaX` and `DeltaY` |
| --- | --- | --- |
| `DragStarted` | The first pressed sample whose distance from the press point is at least the drag threshold. | Distance from the press point. |
| `DragDelta` | Every later pressed sample while dragging. | Distance from the previous sample. |
| `DragCompleted` | The release sample after a drag. | `0` |
| `Tap` | The release sample when no drag started and the release point is closer to the press point than the threshold. | `0` |

The first pressed sample only records the press point and returns no events. A release without a drag that ends at or beyond the threshold distance returns no events.

No framework control consumes these values. The recognizer is a standalone helper: application code feeds it samples and reads the returned kinds.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Tap` | `0` | A press and release without a drag, released within the drag threshold. |
| `DragStarted` | `1` | The pointer moved at least the drag threshold away from the press point. |
| `DragDelta` | `2` | The pointer moved again during a drag. |
| `DragCompleted` | `3` | The pointer was released after a drag. |

## Applies to

`GestureRecognizer` and `GestureEvent`.

## See also

- `Cerneala.UI.Input.GestureRecognizer`
- `Cerneala.UI.Input.GestureEvent`
- `Cerneala.UI.Input.GestureSample`
