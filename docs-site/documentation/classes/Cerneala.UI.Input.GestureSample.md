# GestureSample Struct

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/GestureRecognizer.cs`

Represents a single pointer sample consumed by `GestureRecognizer`.

```csharp
public readonly record struct GestureSample(float X, float Y, bool IsPressed)
```

Inheritance:
`ValueType` -> `GestureSample`

## Examples

Create a sample from pointer coordinates and the current pressed state, then pass it to a recognizer.

```csharp
using Cerneala.UI.Input;

GestureRecognizer recognizer = new();
GestureSample sample = new(pointerX, pointerY, isPressed: true);

IReadOnlyList<GestureEvent> events = recognizer.Process(sample);
```

## Remarks

`GestureSample` is an immutable record struct. `X` and `Y` store the pointer location for the sample. `IsPressed` identifies whether the pointer button is currently pressed.

`GestureRecognizer` uses a transition from not-tracked to pressed as the beginning of a gesture sequence and a later unpressed sample as the release point.

## Constructors

| Name | Description |
| --- | --- |
| `GestureSample(float, float, bool)` | Initializes a gesture sample with pointer coordinates and pressed state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsPressed` | `bool` | Gets whether the pointer is pressed for this sample. |
| `X` | `float` | Gets the sample X coordinate. |
| `Y` | `float` | Gets the sample Y coordinate. |

## Applies to

- `Cerneala.UI.Input.GestureSample`

## See also

- `Cerneala.UI.Input.GestureRecognizer`
- `Cerneala.UI.Input.GestureEvent`
