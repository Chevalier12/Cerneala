# MouseWheelEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/MouseWheelEventArgs.cs`

Provides routed mouse wheel event data with pointer coordinates and wheel delta.

```csharp
public sealed class MouseWheelEventArgs : MouseEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs` -> `MouseEventArgs` -> `MouseWheelEventArgs`

## Examples

Use `Delta` to react to mouse wheel movement while still reading the event coordinates from `MouseEventArgs`.

```csharp
using Cerneala.UI.Input;

void OnMouseWheel(object? sender, MouseWheelEventArgs args)
{
    ZoomAt(args.X, args.Y, args.Delta);
}
```

## Remarks

`MouseWheelEventArgs` extends `MouseEventArgs` with `Delta`, the wheel movement associated with the routed event.

The retained input dispatcher creates this type for preview and bubbling mouse wheel routed events.

## Constructors

| Name | Description |
| --- | --- |
| `MouseWheelEventArgs(RoutedEvent, object, int, int, int)` | Initializes routed mouse wheel event data for an event, original source, coordinates, and wheel delta. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Delta` | `int` | Gets the mouse wheel movement associated with the event. |

## Applies to

- `Cerneala.UI.Input.MouseWheelEventArgs`

## See also

- `Cerneala.UI.Input.MouseEventArgs`
- `Cerneala.UI.Input.InputEvents`
- `Cerneala.UI.Input.RoutedEventArgs`
