# RoutedEventRegistry Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutedEventRegistry.cs`

Registers routed event metadata after validating the routing strategy and event argument type.

```csharp
public static class RoutedEventRegistry
```

Inheritance:
`Object` -> `RoutedEventRegistry`

## Examples

Register a routed event for an owner type with routed event argument data.

```csharp
using Cerneala.UI.Input;

public static readonly RoutedEvent PreviewActionEvent =
    RoutedEventRegistry.Register(
        "PreviewAction",
        typeof(MyControl),
        RoutingStrategy.Tunnel,
        typeof(RoutedEventArgs));
```

## Remarks

`RoutedEventRegistry` centralizes validation for routed event creation. `Register` rejects unsupported `RoutingStrategy` values and requires the argument type to derive from `RoutedEventArgs`.

The method returns a new `RoutedEvent` instance. It does not store global registration state.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Register(string, Type, RoutingStrategy, Type)` | `RoutedEvent` | Creates a routed event after validating the routing strategy and event argument type. Throws if `argsType` is `null`, if `routingStrategy` is unsupported, or if `argsType` does not derive from `RoutedEventArgs`. |

## Applies to

- `Cerneala.UI.Input.RoutedEventRegistry`

## See also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.RoutingStrategy`
