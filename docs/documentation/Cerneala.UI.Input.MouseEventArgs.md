# MouseEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/MouseEventArgs.cs`

Provides routed mouse event data with integer pointer coordinates.

```csharp
public class MouseEventArgs : RoutedEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs` -> `MouseEventArgs`

## Examples

Use `MouseEventArgs` in routed mouse handlers to read the pointer coordinates supplied with the event.

```csharp
using Cerneala.UI.Input;

void OnMouseMove(object? sender, MouseEventArgs args)
{
    int x = args.X;
    int y = args.Y;
}
```

## Remarks

`MouseEventArgs` extends `RoutedEventArgs` with `X` and `Y` coordinates. The coordinates are supplied by the retained input dispatcher when it raises mouse move or related mouse events.

Derived mouse event argument types can add more event-specific data while keeping the same coordinate properties.

## Constructors

| Name | Description |
| --- | --- |
| `MouseEventArgs(RoutedEvent, object, int, int)` | Initializes routed mouse event data for an event, original source, and coordinates. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `int` | Gets the mouse X coordinate associated with the event. |
| `Y` | `int` | Gets the mouse Y coordinate associated with the event. |

## Applies to

- `Cerneala.UI.Input.MouseEventArgs`

## See also

- `Cerneala.UI.Input.MouseButtonEventArgs`
- `Cerneala.UI.Input.MouseWheelEventArgs`
- `Cerneala.UI.Input.RoutedEventArgs`
