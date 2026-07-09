# StylusInputPoint Record

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/StylusInputBridge.cs`

Represents one stylus sample delivered to the retained UI input system.

```csharp
public sealed record StylusInputPoint(
    int Id,
    float X,
    float Y,
    StylusInputAction Action,
    float Pressure = 0.5f,
    bool IsInRange = true,
    string? Button = null);
```

Inheritance:
`Object` -> `StylusInputPoint`

## Examples

Create stylus points for a down/move/up stroke and pass them to an `InkCanvas`.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

InkCanvas canvas = new();

canvas.ApplyStylus(new StylusInputPoint(1, 10, 20, StylusInputAction.Down));
canvas.ApplyStylus(new StylusInputPoint(1, 14, 24, StylusInputAction.Move));
canvas.ApplyStylus(new StylusInputPoint(1, 18, 28, StylusInputAction.Up));
```

Create a pressure-bearing point for routed stylus dispatch.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(100, 100);
StylusInputBridge bridge = new();

StylusInputPoint point = new(9, 11, 13, StylusInputAction.Down, Pressure: 0.75f);
bridge.Dispatch(root, new StylusInputFrame(point));
```

## Remarks

`StylusInputPoint` is an immutable record value used by `StylusInputFrame`, `StylusInputBridge`, `StylusEventArgs`, and `InkCanvas`.

`Id` identifies the stylus contact or device stream. `InkCanvas` uses the value with the stylus input kind to keep active strokes separate.

`X` and `Y` are the coordinates used for hit testing by `StylusInputBridge` and for stroke points when passed to `InkCanvas.ApplyStylus`.

`Action` selects the routed stylus event pair raised by `StylusInputBridge`. `Down`, `Move`, `Up`, `InRange`, `OutOfRange`, `ButtonDown`, and `ButtonUp` map to the corresponding preview and bubbling stylus events in `InputEvents`.

`Pressure` defaults to `0.5f`. `IsInRange` defaults to `true`. `Button` is optional and is used for button actions such as a barrel button.

## Constructors

| Name | Description |
| --- | --- |
| `StylusInputPoint(int, float, float, StylusInputAction, float, bool, string?)` | Initializes a stylus input point with an identifier, coordinates, action, pressure, range state, and optional button name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `int` | Identifies the stylus contact or stream. |
| `X` | `float` | Gets the horizontal coordinate used by hit testing and ink stroke recording. |
| `Y` | `float` | Gets the vertical coordinate used by hit testing and ink stroke recording. |
| `Action` | `StylusInputAction` | Gets the stylus action represented by the point. |
| `Pressure` | `float` | Gets the pressure value associated with the point. The default constructor value is `0.5f`. |
| `IsInRange` | `bool` | Gets whether the stylus is in range. The default constructor value is `true`. |
| `Button` | `string?` | Gets the optional stylus button name for button input actions. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |
| `GetHashCode()` | `int` | Returns a hash code based on the record value. |
| `Equals(object?)` | `bool` | Determines whether the supplied object is equal to this record value. |
| `Equals(StylusInputPoint?)` | `bool` | Determines whether another `StylusInputPoint` has the same record value. |
| `Deconstruct(out int, out float, out float, out StylusInputAction, out float, out bool, out string?)` | `void` | Deconstructs the positional record properties into separate values. |

## Operators

| Name | Return Type | Description |
| --- | --- | --- |
| `operator ==(StylusInputPoint?, StylusInputPoint?)` | `bool` | Determines whether two stylus input points have the same record value. |
| `operator !=(StylusInputPoint?, StylusInputPoint?)` | `bool` | Determines whether two stylus input points have different record values. |

## Applies to

- `Cerneala.UI.Input.StylusInputPoint`

## See also

- `Cerneala.UI.Input.StylusInputBridge`
- `Cerneala.UI.Input.StylusInputFrame`
- `Cerneala.UI.Input.StylusInputAction`
- `Cerneala.UI.Input.StylusEventArgs`
- `Cerneala.UI.Controls.InkCanvas`
