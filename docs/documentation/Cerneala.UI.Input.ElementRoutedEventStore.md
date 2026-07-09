# ElementRoutedEventStore Class

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/ElementRoutedEventStore.cs`

Provides a routed-event handler facade over a `Cerneala.UI.Elements.UIElement` handler store.

```csharp
public sealed class ElementRoutedEventStore
```

Inheritance:  
`object` -> `ElementRoutedEventStore`

## Examples

Registers a mouse-down routed-event handler for a retained `UIElement` and reads the registered handlers back.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement element = new();
ElementRoutedEventStore store = new(element);

store.AddHandler(InputEvents.MouseDownEvent, (sender, args) =>
{
    args.Handled = true;
});

IReadOnlyList<RoutedEventHandler> handlers = store.GetHandlers(InputEvents.MouseDownEvent);
```

## Remarks

`ElementRoutedEventStore` is a small adapter around the associated element's `UIElement.Handlers` property. It does not own a separate handler collection and does not route events by itself; it forwards registration and lookup to the element-level `ElementHandlerStore`.

Adding a handler appends it to the element's handler list for the supplied `RoutedEvent`. When the element is attached to a root, the underlying handler store invalidates hit testing with the reason `"Input handler added"`.

`GetHandlers` returns the handlers currently registered for the routed event. The underlying store returns a snapshot array when handlers exist, or an empty list when none are registered.

The constructor throws `ArgumentNullException` when `element` is `null`. `AddHandler` and `GetHandlers` rely on the underlying `ElementHandlerStore` validation and throw `ArgumentNullException` for a `null` routed event; `AddHandler` also throws for a `null` handler.

## Constructors

| Name | Description |
| --- | --- |
| `ElementRoutedEventStore(UIElement element)` | Initializes a store facade for the specified element. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AddHandler(RoutedEvent routedEvent, RoutedEventHandler handler)` | `void` | Registers a routed-event handler on the associated element. |
| `GetHandlers(RoutedEvent routedEvent)` | `IReadOnlyList<RoutedEventHandler>` | Gets the handlers registered on the associated element for the specified routed event. |

## Applies to

Cerneala retained UI input routing.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.ElementHandlerStore`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventHandler`
- `Cerneala.UI.Input.RoutedEventRouter`
