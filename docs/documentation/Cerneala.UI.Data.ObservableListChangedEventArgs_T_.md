# ObservableListChangedEventArgs<T> Class

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IObservableList{T}.cs`

Observable list implementation: `UI/Data/ObservableList{T}.cs`

Describes a typed change notification raised by an observable list.

```csharp
public sealed class ObservableListChangedEventArgs<T> : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `ObservableListChangedEventArgs<T>`

## Examples

Observe item additions and read the typed payload.

```csharp
using Cerneala.UI.Data;

ObservableList<string> items = new();
items.Changed += (_, args) =>
{
    if (args.Kind == ObservableListChangeKind.Add)
    {
        string addedItem = args.Item;
        int index = args.Index;
    }
};

items.Add("Ink");
```

Read replacement data from the event arguments.

```csharp
using Cerneala.UI.Data;

ObservableList<string> items = new(["Old"]);
items.Changed += (_, args) =>
{
    if (args.Kind == ObservableListChangeKind.Replace)
    {
        string oldValue = args.OldItem;
        string newValue = args.Item;
    }
};

items[0] = "New";
```

## Remarks

`ObservableListChangedEventArgs<T>` is the typed event payload used by `IObservableList<T>.Changed`, `ObservableList<T>.Changed`, and `CollectionView<T>.Changed`.

The class stores change metadata only; it does not perform list mutation or validation. The constructor copies the supplied scalar values directly. When `items` or `oldItems` is `null`, the corresponding property is set to an empty read-only list.

`ObservableList<T>` fills the payload according to the mutation that raised the notification:

| Change kind | Index data | Item data |
| --- | --- | --- |
| `Add` | `Index` is the inserted index. | `Item` and `Items` contain the added item. |
| `Remove` | `Index` is the removed index. | `Item`, `OldItem`, and `OldItems` contain the removed item. |
| `Replace` | `Index` is the replaced index. | `Item` and `Items` contain the new item; `OldItem` and `OldItems` contain the old item. |
| `Move` | `Index` is the new index and `OldIndex` is the previous index. | `Item`, `OldItem`, `Items`, and `OldItems` contain the moved item. |
| `Clear` | `Index` and `OldIndex` keep their default value of `-1`. | `OldItems` contains the removed snapshot. |
| `Reset` | `Index` and `OldIndex` keep their default value of `-1`. | `Items` contains the new snapshot and `OldItems` contains the previous snapshot when supplied by the source. |

The non-generic `IObservableList` notification path translates this typed payload to `ObservableListChangedEventArgs` by casting `Items` and `OldItems` to `object?`.

## Constructors

| Name | Description |
| --- | --- |
| `ObservableListChangedEventArgs(ObservableListChangeKind kind, int index = -1, int oldIndex = -1, T item = default!, T oldItem = default!, IReadOnlyList<T>? items = null, IReadOnlyList<T>? oldItems = null)` | Initializes a typed list-change payload. `items` and `oldItems` become empty lists when omitted or `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `ObservableListChangeKind` | Gets the kind of list change represented by this notification. |
| `Index` | `int` | Gets the primary index for the change, or `-1` when the notification has no primary index. |
| `OldIndex` | `int` | Gets the previous index for move notifications, or `-1` when the notification has no previous index. |
| `Item` | `T` | Gets the current or affected item supplied by the event source. |
| `OldItem` | `T` | Gets the previous item supplied by the event source. |
| `Items` | `IReadOnlyList<T>` | Gets the current, added, moved, replaced, or reset item snapshot supplied by the event source. Empty when omitted. |
| `OldItems` | `IReadOnlyList<T>` | Gets the removed or previous item snapshot supplied by the event source. Empty when omitted. |

## Applies to

`Cerneala` UI data collection notifications.

## See also

- `ObservableList<T>`
- `ObservableListChangedEventArgs`
- `ObservableListChangeKind`
- `IObservableList<T>`
- `CollectionView<T>`
