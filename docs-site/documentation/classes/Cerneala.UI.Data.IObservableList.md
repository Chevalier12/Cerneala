# IObservableList Interface

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IObservableList{T}.cs`

Provides the `Cerneala.UI.Data.IObservableList` API surface.

```csharp
public interface IObservableList : IEnumerable
```

## Events

| Name | Type | Description |
| --- | --- | --- |
| `Changed` | `EventHandler<ObservableListChangedEventArgs>?` | Raised when the collection changes. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of items. |
| `this[int index]` | `object?` | Gets the item at the specified index. |

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
