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

element.Handlers.AddHandler(
    InputEvents.MouseUpEvent,
    OnMouseUp,
    handledEventsToo: true);
IReadOnlyList<RoutedEventHandler> handlers =
    element.Handlers.GetHandlers(InputEvents.MouseUpEvent);
```

## Remarks

`ElementHandlerStore` keeps handlers grouped by `RoutedEvent`. Adding a handler creates the event list when necessary, records whether it accepts already-handled events through `handledEventsToo`, appends the handler, and invalidates the owning element's hit-test route when the element has a root.

`RemoveHandler` removes a single matching handler. When the last handler for a routed event is removed, the routed event entry is removed from the store. Successful removals also invalidate the hit-test route.

`GetHandlers` returns a snapshot array for the requested routed event, so callers do not mutate the internal list. It returns handlers only, without their `handledEventsToo` flags. `EnumerateHandlers` yields each routed event, handler, and `HandledEventsToo` flag currently registered.

## Methods

| Name | Description |
| --- | --- |
| `AddHandler(RoutedEvent, RoutedEventHandler, bool handledEventsToo = false)` | Adds a routed event handler, optionally allowing invocation after the routed event is already handled, and invalidates input routing when the owner is rooted. |
| `RemoveHandler(RoutedEvent, RoutedEventHandler)` | Removes a routed event handler and returns whether a handler was removed. |
| `GetHandlers(RoutedEvent)` | Returns the handlers registered for a routed event as a read-only snapshot. |
| `EnumerateHandlers()` | Enumerates every routed event, handler, and `HandledEventsToo` flag in the store. |

## Handler Registration Details

| Member | Behavior |
| --- | --- |
| `handledEventsToo` | Defaults to `false`; when `true`, the routed-event router may invoke the handler even after the event has been marked handled. |
| `GetHandlers` | Returns only the delegate values and does not expose registration flags. |
| `EnumerateHandlers` | Returns `(RoutedEvent, Handler, HandledEventsToo)` tuples, preserving the registration flag. |

## Applies to

Cerneala retained UI routed input.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventHandler`
