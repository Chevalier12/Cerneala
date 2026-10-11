# RoutedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutedEventArgs.cs`

Provides base data for routed input events, including routed event identity, original source, current source, and handled state.

```csharp
public class RoutedEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs`

## Examples

Mark a routed event as handled inside a handler.

```csharp
using Cerneala.UI.Input;

void OnMouseDown(object? sender, RoutedEventArgs args)
{
    if (CanHandle(args.RoutedEvent))
    {
        args.Handled = true;
    }
}
```

## Remarks

`RoutedEventArgs` is the common base class for routed input event data. `RoutedEvent` identifies the event being raised. `OriginalSource` stores the object supplied when the event data was created. `Source` starts as `OriginalSource` and may be updated by routing code as the event moves through an input route.

`Handled` can be set by any handler. It does not stop routing: `RoutedEventRouter` still visits the remaining elements of the route, but on them it runs only handlers registered with `handledEventsToo: true`. Some framework code reads `Handled` after an event to decide what to do next, for example the input bridge runs a button command only when the mouse-up events were not handled.

The two-parameter constructor throws when `routedEvent` or `originalSource` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedEventArgs()` | Initializes empty routed event data. `RoutedEvent`, `OriginalSource`, and `Source` stay `null` until set; `RoutedEventRouter.Raise` needs `RoutedEvent` to be set. |
| `RoutedEventArgs(RoutedEvent, object)` | Initializes routed event data for an event and original source. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Handled` | `bool` | Gets or sets whether the event has been handled. |
| `OriginalSource` | `object` | Gets the original event source supplied to the constructor. |
| `RoutedEvent` | `RoutedEvent` | Gets or sets the routed event associated with this event data. |
| `Source` | `object` | Gets or sets the current source during event routing. |

## Applies to

- `Cerneala.UI.Input.RoutedEventArgs`

## See also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Input.MouseEventArgs`
