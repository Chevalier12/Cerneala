# ObservableList<T> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/ObservableList{T}.cs`

Represents a mutable generic list that raises typed and untyped change notifications.

```csharp
public sealed class ObservableList<T> : IObservableList<T>, IObservableList, IList<T>
```

Implements:
`IObservableList<T>`, `IObservableList`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IEnumerable`

## Examples

```csharp
using Cerneala.UI.Data;

ObservableList<string> items = new(["One"]);
items.Changed += (_, args) =>
{
    ObservableListChangeKind kind = args.Kind;
    int index = args.Index;
};

items.Add("Two");
items.Move(0, 1);
items.ReplaceWith(["A", "B"]);
```

## Remarks

`ObservableList<T>` wraps an internal `List<T>` and emits `ObservableListChangedEventArgs<T>` for mutations. It also implements the untyped `IObservableList` event by translating typed payloads to `object?` collections.

Setting an item by index raises a `Replace` notification only when the new value is not equal to the old value according to `EqualityComparer<T>.Default`.

`Move` validates both indices and does nothing when the source and destination are the same. `Clear` does nothing for an already empty list. `ReplaceWith` replaces the complete contents and emits a `Reset` notification containing the new and old item snapshots.

## Constructors

| Name | Description |
| --- | --- |
| `ObservableList()` | Initializes an empty observable list. |
| `ObservableList(IEnumerable<T>)` | Initializes the list with the supplied items. |

## Properties

| Name | Description |
| --- | --- |
| `Count` | Gets the current number of items. |
| `IsReadOnly` | Gets `false`; the list is mutable. |
| `this[int index]` | Gets or replaces the item at the specified index. |

## Methods

| Name | Description |
| --- | --- |
| `Add(T)` | Adds an item and raises an `Add` notification. |
| `Insert(int, T)` | Inserts an item at an index and raises an `Add` notification. |
| `Remove(T)` | Removes the first matching item and raises a `Remove` notification when found. |
| `RemoveAt(int)` | Removes the item at an index and raises a `Remove` notification. |
| `Move(int, int)` | Moves an item from one index to another and raises a `Move` notification. |
| `Clear()` | Removes all items and raises a `Clear` notification when the list was not empty. |
| `ReplaceWith(IEnumerable<T>)` | Replaces the full contents and raises a `Reset` notification. |
| `Contains(T)` | Returns whether the item exists in the list. |
| `CopyTo(T[], int)` | Copies items into an array starting at the specified array index. |
| `IndexOf(T)` | Returns the zero-based index of the first matching item, or `-1` when not found. |
| `GetEnumerator()` | Returns an enumerator over the current items. |

## Events

| Name | Description |
| --- | --- |
| `Changed` | Raised after a typed list mutation. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IObservableList.Changed` | Adds or removes untyped change handlers. |
| `IObservableList.this[int]` | Gets an item as `object?`. |
| `IEnumerable.GetEnumerator()` | Returns an untyped enumerator. |

## Applies to

Cerneala retained UI data binding and item controls.

## See also

- `Cerneala.UI.Data.ObservableListChangedEventArgs<T>`
- `Cerneala.UI.Data.IObservableList<T>`
- `Cerneala.UI.Controls.ItemsControl`
