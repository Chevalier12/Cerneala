# StylusEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/StylusInputBridge.cs`

Provides routed stylus event data for a single stylus input point.

```csharp
public sealed class StylusEventArgs : RoutedEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs` -> `StylusEventArgs`

## Examples

Create stylus event data from a `StylusInputPoint` and read the stylus-specific values exposed by the event args.

```csharp
using Cerneala.UI.Input;

StylusInputPoint point = new(
    Id: 7,
    X: 128f,
    Y: 64f,
    Action: StylusInputAction.Down,
    Pressure: 0.75f);

StylusEventArgs args = new(
    InputEvents.StylusDownEvent,
    originalSource: "ink-surface",
    point);

int stylusId = args.StylusId;
float pressure = args.Pressure;
```

## Remarks

`StylusEventArgs` extends `RoutedEventArgs` with a `StylusInputPoint` and convenience properties for the point's identity, coordinates, pressure, range state, and button name.

`StylusInputBridge` creates `StylusEventArgs` when it dispatches stylus input through the retained routed event system. The bridge raises matching preview and bubbling routed events for `Down`, `Move`, `Up`, `InRange`, `OutOfRange`, `ButtonDown`, and `ButtonUp` stylus actions.

The constructor requires a non-null routed event, original source, and stylus point. `RoutedEventArgs` validates the routed event and original source; `StylusEventArgs` validates the `point` argument.

## Constructors

| Name | Description |
| --- | --- |
| `StylusEventArgs(RoutedEvent, object, StylusInputPoint)` | Initializes routed stylus event data for an event, original source, and stylus point. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Point` | `StylusInputPoint` | Gets the stylus input point associated with the event. |
| `StylusId` | `int` | Gets the stylus identifier from `Point.Id`. |
| `X` | `float` | Gets the stylus X coordinate from `Point.X`. |
| `Y` | `float` | Gets the stylus Y coordinate from `Point.Y`. |
| `Pressure` | `float` | Gets the stylus pressure from `Point.Pressure`. |
| `IsInRange` | `bool` | Gets whether the stylus point is in range from `Point.IsInRange`. |
| `Button` | `string?` | Gets the stylus button name from `Point.Button`, or `null` when no button is associated with the point. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event being raised. |
| `OriginalSource` | `object` | Gets the original event source supplied at construction time. |
| `Source` | `object` | Gets or sets the current event source during routing. |
| `Handled` | `bool` | Gets or sets whether the routed event has been handled. |

## Applies to

- `Cerneala.UI.Input.StylusEventArgs`

## See also

- `Cerneala.UI.Input.StylusInputBridge`
- `Cerneala.UI.Input.StylusInputPoint`
- `Cerneala.UI.Input.StylusInputAction`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.InputEvents`
