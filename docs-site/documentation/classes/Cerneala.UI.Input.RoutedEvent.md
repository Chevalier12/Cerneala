# RoutedEvent Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutedEvent.cs`

Represents the metadata that identifies a routed input event and describes how it travels through the input tree.

```csharp
public sealed class RoutedEvent
```

Inheritance:
`object` -> `RoutedEvent`

## Examples

Register a bubbling routed event and raise it through a `UiInputTree`.

```csharp
using Cerneala.UI.Input;

RoutedEvent clickEvent = RoutedEventRegistry.Register(
    "Click",
    typeof(MyControl),
    RoutingStrategy.Bubble,
    typeof(RoutedEventArgs));

UiInputTree tree = new();
UiElementId rootId = new("root");
UiElementId childId = new("child");

tree.Add(rootId, parentId: null);
tree.Add(childId, parentId: rootId);
tree.AddHandler(childId, clickEvent, (sender, args) => args.Handled = true);

RoutedEventRouter.Raise(tree, childId, new RoutedEventArgs(clickEvent, childId));
```

## Remarks

`RoutedEvent` is a sealed metadata object. It stores the event name, owner type, routing strategy, and event-argument type. The constructor is internal; callers create instances through `RoutedEventRegistry.Register`.

`RoutedEventRegistry.Register` validates that the event-argument type derives from `RoutedEventArgs`, that the routing strategy is a defined `RoutingStrategy` value, and that the argument type is not null. The `RoutedEvent` constructor validates that `Name` is not empty or whitespace and that `OwnerType` and `ArgsType` are not null.

The same `RoutedEvent` instance is used when handlers are registered and when an event is raised. `UiInputTree` stores handlers by `(UiElementId, RoutedEvent)`, and `RoutedEvent` does not override equality.

`RoutingStrategy` controls how `RoutedEventRouter.Raise` builds the route:

| Strategy | Route |
| --- | --- |
| `Direct` | Raises only on the target element. |
| `Bubble` | Raises from the target element toward the root. |
| `Tunnel` | Raises from the root toward the target element. |

If a handler sets `RoutedEventArgs.Handled` to `true`, routing stops.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the routed event name. |
| `OwnerType` | `Type` | Gets the type that owns or registered the routed event. |
| `RoutingStrategy` | `RoutingStrategy` | Gets the routing strategy used when the event is raised. |
| `ArgsType` | `Type` | Gets the expected event-argument type. |

## Applies To

Cerneala routed input events.

## See Also

- `RoutedEventRegistry`
- `RoutedEventArgs`
- `RoutedEventRouter`
- `RoutingStrategy`
