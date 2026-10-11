# IObservableList Interface

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IObservableList{T}.cs`

Represents a read-only list of untyped items that raises a notification when its contents change.

```csharp
public interface IObservableList : IEnumerable
```

## Examples

```csharp
using Cerneala.UI.Data;

ObservableList<int> numbers = new(new[] { 7, 8 });
IObservableList list = numbers;
list.Changed += (_, e) => Console.WriteLine($"{e.Kind} {e.Item} {list.Count} {list[0]}");

numbers.Add(9);
// Add 9 3 7
```

## Remarks

`ObservableList<T>` is the only Cerneala implementation. It implements this interface explicitly, next to `IObservableList<T>`. Each change raises the typed `IObservableList<T>.Changed` first and then this untyped `Changed`, with the same kind, indexes, and items as `object?` values.

`ItemsControl` uses this interface: when `ItemsSource` implements `IObservableList`, the control subscribes to `Changed` and rebuilds its items after each notification. For `ObservableListChangeKind.Reset` and `Clear`, it also clears its item container generator.

`CollectionView<T>` does not implement this interface.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of items. |
| `this[int index]` | `object?` | Gets the item at `index`. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `Changed` | `EventHandler<ObservableListChangedEventArgs>?` | Raised after the list changes. The arguments describe the change; see `ObservableListChangeKind`. |

## Applies to

`ObservableList<T>` and `ItemsControl.ItemsSource`.

## See also

- `Cerneala.UI.Data.IObservableList<T>`
- `Cerneala.UI.Data.ObservableList<T>`
- `Cerneala.UI.Data.ObservableListChangedEventArgs`
- `Cerneala.UI.Data.ObservableListChangeKind`
