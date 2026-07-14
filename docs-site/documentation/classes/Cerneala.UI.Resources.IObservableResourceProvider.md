# IObservableResourceProvider Interface

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/IObservableResourceProvider.cs`

Extends `IResourceProvider` with ordered resource-delta notifications.

```csharp
public interface IObservableResourceProvider : IResourceProvider
```

## Remarks

`UIRoot.SetResourceProvider` subscribes to `ResourceChanged`. UI-thread notifications retain their synchronous behavior. Each off-thread notification is posted to the root Relay and is not coalesced: callbacks retain FIFO enqueue order so successive versions of the same resource are observed in order. Replacing the provider unsubscribes it and makes already queued callbacks from that provider no-ops.

The provider remains responsible for publishing coherent resource state. Relay dispatch protects retained UI state; it does not make the provider's own storage thread-safe.

## Events

| Name | Description |
| --- | --- |
| `ResourceChanged` | Reports one resource delta. Root consumers dispatch off-thread events FIFO through their Relay. |

## Applies to

Cerneala UI runtime and framework API consumers.
