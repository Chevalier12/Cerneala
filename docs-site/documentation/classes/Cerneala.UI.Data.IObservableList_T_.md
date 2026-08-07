# IObservableList<T> Interface

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IObservableList{T}.cs`

Provides the `Cerneala.UI.Data.IObservableList<T>` API surface.

```csharp
public interface IObservableList<T> : IReadOnlyList<T>
```

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The item type stored by the list. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `Changed` | `EventHandler<ObservableListChangedEventArgs<T>>?` | Raised when the collection changes. |

## Inherited Members

`Count`, the integer indexer, and enumeration are inherited from `IReadOnlyList<T>`.

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
