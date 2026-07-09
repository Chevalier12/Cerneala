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

`Handled` is mutable and lets handlers stop or affect later routing behavior.

The constructor throws when `routedEvent` or `originalSource` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedEventArgs(RoutedEvent, object)` | Initializes routed event data for an event and original source. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Handled` | `bool` | Gets or sets whether the event has been handled. |
| `OriginalSource` | `object` | Gets the original event source supplied to the constructor. |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event associated with this event data. |
| `Source` | `object` | Gets or sets the current source during event routing. |

## Applies to

- `Cerneala.UI.Input.RoutedEventArgs`

## See also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Input.MouseEventArgs`
