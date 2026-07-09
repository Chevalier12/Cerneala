# TouchInputPoint Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: [`UI/Input/TouchInputBridge.cs`](../../UI/Input/TouchInputBridge.cs)

Represents one touch sample, including its touch identifier, coordinates, and action.

```csharp
public sealed record TouchInputPoint(int Id, float X, float Y, TouchInputAction Action);
```

Inheritance:
`Object` -> `TouchInputPoint`

Implements:
`IEquatable<TouchInputPoint>`

## Examples

Create a touch frame with one touch-down point and dispatch it through a `TouchInputBridge`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(800, 600);
TouchInputBridge bridge = new();

TouchInputPoint point = new(7, 10, 12, TouchInputAction.Down);
bridge.Dispatch(root, new TouchInputFrame(point));
```

Create a move point for an existing touch id:

```csharp
using Cerneala.UI.Input;

TouchInputPoint move = new(7, 32, 48, TouchInputAction.Move);

int touchId = move.Id;
float x = move.X;
float y = move.Y;
TouchInputAction action = move.Action;
```

## Remarks

`TouchInputPoint` is the per-contact input value consumed by `TouchInputFrame` and `TouchInputBridge`. `TouchInputBridge.Dispatch` iterates the frame's points, hit-tests each point at `X` and `Y`, and routes preview and bubbling touch events based on `Action`.

The `Id` value identifies the touch contact across frames. `TouchInputBridge` also uses the same id for touch capture lookup, so subsequent points with the same id can route to a captured element instead of the current hit-test result.

`Action` must be one of the supported `TouchInputAction` values: `Down`, `Move`, or `Up`. `TouchInputBridge` maps those values to the corresponding preview and bubbling touch routed events.

The type is a sealed record, so its constructor parameters are exposed as immutable init-only properties and instances use record value equality. The source does not perform coordinate or id validation when a point is constructed.

## Constructors

| Name | Description |
| --- | --- |
| `TouchInputPoint(int id, float x, float y, TouchInputAction action)` | Initializes a touch point with a touch id, coordinates, and touch action. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Id` | `int` | Gets the touch contact identifier. |
| `X` | `float` | Gets the horizontal coordinate used for hit testing and routed touch event data. |
| `Y` | `float` | Gets the vertical coordinate used for hit testing and routed touch event data. |
| `Action` | `TouchInputAction` | Gets the touch action routed for this point. |

## Applies to

- `Cerneala.UI.Input.TouchInputPoint`

## See also

- [`TouchInputBridge`](../../UI/Input/TouchInputBridge.cs)
- [`TouchInputFrame`](../../UI/Input/TouchInputBridge.cs)
- [`TouchEventArgs`](../../UI/Input/TouchInputBridge.cs)
- [`InputEvents`](../../UI/Input/InputEvents.cs)
