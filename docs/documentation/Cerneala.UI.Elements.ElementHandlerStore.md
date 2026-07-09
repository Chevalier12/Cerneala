# ElementHandlerStore Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/ElementHandlerStore.cs`

Stores routed event handlers for a `UIElement` and invalidates input routing when handler registrations change.

```csharp
public sealed class ElementHandlerStore
```

## Examples

```csharp
using Cerneala.UI.Input;

element.Handlers.AddHandler(InputEvents.MouseUpEvent, OnMouseUp);
IReadOnlyList<RoutedEventHandler> handlers =
    element.Handlers.GetHandlers(InputEvents.MouseUpEvent);
```

## Remarks

`ElementHandlerStore` keeps handlers grouped by `RoutedEvent`. Adding a handler creates the event list when necessary, appends the handler, and invalidates the owning element's hit-test route when the element has a root.

`RemoveHandler` removes a single matching handler. When the last handler for a routed event is removed, the routed event entry is removed from the store. Successful removals also invalidate the hit-test route.

`GetHandlers` returns a snapshot array for the requested routed event, so callers do not mutate the internal list. `EnumerateHandlers` yields each routed event and handler pair currently registered.

## Methods

| Name | Description |
| --- | --- |
| `AddHandler(RoutedEvent, RoutedEventHandler)` | Adds a routed event handler and invalidates input routing when the owner is rooted. |
| `RemoveHandler(RoutedEvent, RoutedEventHandler)` | Removes a routed event handler and returns whether a handler was removed. |
| `GetHandlers(RoutedEvent)` | Returns the handlers registered for a routed event as a read-only snapshot. |
| `EnumerateHandlers()` | Enumerates every routed event and handler pair in the store. |

## Applies to

Cerneala retained UI routed input.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventHandler`
