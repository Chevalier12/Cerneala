# PointerSnapshot Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/PointerSnapshot.cs`

Represents an immutable snapshot of pointer position, mouse wheel value, and mouse button state at one input sampling point.

```csharp
public sealed class PointerSnapshot
```

Inheritance:
`Object` -> `PointerSnapshot`

## Examples

Create a pointer snapshot from the empty state, then query position, wheel value, and button state:

```csharp
using Cerneala.UI.Input;

PointerSnapshot snapshot = PointerSnapshot.Empty
    .WithPosition(120, 64)
    .WithWheelValue(360)
    .WithButton(InputMouseButton.Left, isDown: true);

float x = snapshot.X;
float y = snapshot.Y;
int wheelValue = snapshot.WheelValue;
bool leftIsDown = snapshot.IsDown(InputMouseButton.Left);
bool noneIsDown = snapshot.IsDown(InputMouseButton.None);
```

`leftIsDown` is `true`. `noneIsDown` is `false` because `InputMouseButton.None` is treated as a sentinel and is never stored as a down button.

## Remarks

`PointerSnapshot` stores pointer state as a value-like immutable object. Its public update methods return a new snapshot with the requested change, while preserving the other values from the current instance.

`InputFrame` uses previous and current `PointerSnapshot` instances to report pointer position, button transitions, current wheel value, and wheel delta. `InputFrame.PointerFrame.WheelDelta` is calculated from the current snapshot's `WheelValue` minus the previous snapshot's `WheelValue`.

`Empty` has position `(0, 0)`, wheel value `0`, and no buttons down. `WithButton` ignores `InputMouseButton.None` and returns the same instance for that sentinel value.

The type has no public constructor. Instances are created from `Empty` by chaining `WithPosition`, `WithWheelValue`, and `WithButton`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Empty` | `PointerSnapshot` | Gets a snapshot at position `(0, 0)` with wheel value `0` and no buttons down. |
| `WheelValue` | `int` | Gets the absolute mouse wheel value stored in this snapshot. |
| `X` | `float` | Gets the pointer X coordinate stored in this snapshot. |
| `Y` | `float` | Gets the pointer Y coordinate stored in this snapshot. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsDown(InputMouseButton button)` | `bool` | Returns `true` when `button` is stored as down; always returns `false` for `InputMouseButton.None`. |
| `WithButton(InputMouseButton button, bool isDown)` | `PointerSnapshot` | Returns a snapshot with `button` set to the supplied down state. Returns the same instance when `button` is `InputMouseButton.None`. |
| `WithPosition(float x, float y)` | `PointerSnapshot` | Returns a snapshot with the supplied pointer coordinates and the current wheel and button state. |
| `WithWheelValue(int wheelValue)` | `PointerSnapshot` | Returns a snapshot with the supplied wheel value and the current position and button state. |

## Applies to

Project: `Cerneala`

Input namespace: `Cerneala.UI.Input`

## See also

- `InputFrame`
- `InputMouseButton`
- `MonoGameInputSource`
- `UI/Input/PointerSnapshot.cs`
