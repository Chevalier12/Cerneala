# IObservableList<T> Interface

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IObservableList{T}.cs`

Represents a read-only list of typed items that raises a notification when its contents change.

```csharp
public interface IObservableList<T> : IReadOnlyList<T>
```

## Examples

```csharp
using Cerneala.UI.Data;

ObservableList<int> numbers = new();
IObservableList<int> list = numbers;
list.Changed += (_, e) => Console.WriteLine($"{e.Kind} {e.Item} {list.Count}");

numbers.Add(42);
// Add 42 1
```

## Remarks

`ObservableList<T>` is the only Cerneala implementation. It raises this typed `Changed` first and then the untyped `IObservableList.Changed` for the same change.

`CollectionView<T>` checks whether its source implements `IObservableList<T>`. If it does, the view subscribes to `Changed` and rebuilds itself after every change, whatever its kind. `Dispose` removes that subscription.

## Events

| Name | Type | Description |
| --- | --- | --- |
| `Changed` | `EventHandler<ObservableListChangedEventArgs<T>>?` | Raised after the list changes. The arguments describe the change; see `ObservableListChangeKind`. |

Members inherited from `IReadOnlyList<T>` (`Count`, the indexer, and enumeration) are not repeated here.

## Applies to

`ObservableList<T>` and `CollectionView<T>`.

## See also

- `Cerneala.UI.Data.IObservableList`
- `Cerneala.UI.Data.ObservableList<T>`
- `Cerneala.UI.Data.ObservableListChangedEventArgs<T>`
- `Cerneala.UI.Data.CollectionView<T>`
