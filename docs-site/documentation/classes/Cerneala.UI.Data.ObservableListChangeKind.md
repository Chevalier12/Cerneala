# ObservableListChangeKind Enum

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/ObservableListChangeKind.cs`

Specifies what kind of change an observable list reports.

```csharp
public enum ObservableListChangeKind
```

## Examples

```csharp
using Cerneala.UI.Data;

ObservableList<string> list = new(new[] { "a", "b" });
list.Changed += (_, e) => Console.WriteLine(
    $"{e.Kind} Index={e.Index} OldIndex={e.OldIndex} Items=[{string.Join(",", e.Items)}] OldItems=[{string.Join(",", e.OldItems)}]");

list.Add("c");
list.Move(0, 2);
list[0] = "x";
list.Remove("x");
list.ReplaceWith(new[] { "p", "q" });
list.Clear();
// Add Index=2 OldIndex=-1 Items=[c] OldItems=[]
// Move Index=2 OldIndex=0 Items=[a] OldItems=[a]
// Replace Index=0 OldIndex=-1 Items=[x] OldItems=[b]
// Remove Index=0 OldIndex=-1 Items=[] OldItems=[x]
// Reset Index=-1 OldIndex=-1 Items=[p,q] OldItems=[c,a]
// Clear Index=-1 OldIndex=-1 Items=[] OldItems=[p,q]
```

## Remarks

`ObservableListChangedEventArgs` and `ObservableListChangedEventArgs<T>` carry the kind in `Kind`. The table shows what `ObservableList<T>` reports for each kind:

| Kind | Raised by | `Index` | `OldIndex` | `Items` | `OldItems` | No event when |
| --- | --- | --- | --- | --- | --- | --- |
| `Add` | `Add`, `Insert` | New index | `-1` | The added item | Empty | — |
| `Remove` | `Remove`, `RemoveAt` | Old index | `-1` | Empty | The removed item | `Remove` finds no match. |
| `Replace` | Indexer set | Index | `-1` | The new item | The old item | The new value equals the old one. |
| `Move` | `Move` | New index | Old index | The moved item | The moved item | Both indexes are equal. |
| `Reset` | `ReplaceWith` | `-1` | `-1` | All new items | All old items | — |
| `Clear` | `Clear` | `-1` | `-1` | Empty | All old items | The list is already empty. |

`CollectionView<T>.Changed` always reports `Reset`, with the whole new view in `Items` and the whole old view in `OldItems`.

`ItemsControl` rebuilds its items after every kind. For `Reset` and `Clear` it also clears its item container generator.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Add` | `0` | One item was added. |
| `Remove` | `1` | One item was removed. |
| `Replace` | `2` | One item was replaced by another. |
| `Move` | `3` | One item moved to another index. |
| `Reset` | `4` | The whole contents were replaced. |
| `Clear` | `5` | All items were removed. |

## Applies to

`ObservableListChangedEventArgs`, `ObservableListChangedEventArgs<T>`, `ObservableList<T>`, and `CollectionView<T>`.

## See also

- `Cerneala.UI.Data.ObservableList<T>`
- `Cerneala.UI.Data.IObservableList`
- `Cerneala.UI.Data.IObservableList<T>`
- `Cerneala.UI.Data.CollectionView<T>`
