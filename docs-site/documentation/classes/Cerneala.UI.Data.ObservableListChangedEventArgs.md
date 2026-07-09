# ObservableListChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IObservableList{T}.cs`

Describes an untyped change notification raised by an observable list.

```csharp
public sealed class ObservableListChangedEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `ObservableListChangedEventArgs`

## Examples

Observe list additions through the non-generic `IObservableList` interface.

```csharp
using Cerneala.UI.Data;

ObservableList<string> typedItems = new();
IObservableList items = typedItems;

items.Changed += (_, args) =>
{
    if (args.Kind == ObservableListChangeKind.Add)
    {
        object? addedItem = args.Item;
        int index = args.Index;
    }
};

typedItems.Add("Ink");
```

Read replacement data without knowing the item type at compile time.

```csharp
using Cerneala.UI.Data;

ObservableList<string> typedItems = new(["Old"]);
IObservableList items = typedItems;

items.Changed += (_, args) =>
{
    if (args.Kind == ObservableListChangeKind.Replace)
    {
        object? oldValue = args.OldItem;
        object? newValue = args.Item;
    }
};

typedItems[0] = "New";
```

## Remarks

`ObservableListChangedEventArgs` is the untyped event payload used by `IObservableList.Changed`. `ObservableList<T>` creates it from the typed `ObservableListChangedEventArgs<T>` notification by copying the change kind, indexes, current item, old item, and item snapshots as `object?` values.

The class stores change metadata only; it does not mutate the list or validate indexes. The constructor copies the supplied scalar values directly. When `items` or `oldItems` is `null`, the corresponding property is set to an empty read-only list.

`ObservableList<T>` fills the untyped payload according to the mutation that raised the notification:

| Change kind | Index data | Item data |
| --- | --- | --- |
| `Add` | `Index` is the inserted index. | `Item` and `Items` contain the added item. |
| `Remove` | `Index` is the removed index. | `Item`, `OldItem`, and `OldItems` contain the removed item. |
| `Replace` | `Index` is the replaced index. | `Item` and `Items` contain the new item; `OldItem` and `OldItems` contain the old item. |
| `Move` | `Index` is the new index and `OldIndex` is the previous index. | `Item`, `OldItem`, `Items`, and `OldItems` contain the moved item. |
| `Clear` | `Index` and `OldIndex` keep their default value of `-1`. | `OldItems` contains the removed snapshot. |
| `Reset` | `Index` and `OldIndex` keep their default value of `-1`. | `Items` contains the new snapshot and `OldItems` contains the previous snapshot. |

## Constructors

| Name | Description |
| --- | --- |
| `ObservableListChangedEventArgs(ObservableListChangeKind kind, int index = -1, int oldIndex = -1, object? item = null, object? oldItem = null, IReadOnlyList<object?>? items = null, IReadOnlyList<object?>? oldItems = null)` | Initializes an untyped list-change payload. `items` and `oldItems` become empty lists when omitted or `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `ObservableListChangeKind` | Gets the kind of list change represented by this notification. |
| `Index` | `int` | Gets the primary index for the change, or `-1` when the notification has no primary index. |
| `OldIndex` | `int` | Gets the previous index for move notifications, or `-1` when the notification has no previous index. |
| `Item` | `object?` | Gets the current or affected item supplied by the event source. |
| `OldItem` | `object?` | Gets the previous item supplied by the event source. |
| `Items` | `IReadOnlyList<object?>` | Gets the current, added, moved, replaced, or reset item snapshot supplied by the event source. Empty when omitted. |
| `OldItems` | `IReadOnlyList<object?>` | Gets the removed or previous item snapshot supplied by the event source. Empty when omitted. |

## Applies to

`Cerneala` UI data collection notifications.

## See also

- `Cerneala.UI.Data.IObservableList`
- `Cerneala.UI.Data.ObservableList<T>`
- `Cerneala.UI.Data.ObservableListChangedEventArgs<T>`
- `Cerneala.UI.Data.ObservableListChangeKind`
