# FilterPredicate<T> Delegate

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/FilterPredicate{T}.cs`

Represents a test that decides whether a source item appears in a `CollectionView<T>`.

```csharp
public delegate bool FilterPredicate<in T>(T item);
```

## Examples

```csharp
using Cerneala.UI.Data;

CollectionView<int> view = new(new[] { 1, 2, 3, 4, 5 });
view.Filter = value => value % 2 == 0;
Console.WriteLine(string.Join(", ", view));
// 1, 2, 3, 4, 5

view.Refresh();
Console.WriteLine(string.Join(", ", view));
// 2, 4
```

Setting `Filter` does not change the view. The filter applies after `Refresh()`.

## Remarks

Return `true` to keep an item and `false` to leave it out. A `null` filter keeps every item.

`CollectionView<T>.Refresh` rebuilds the view from the source: it applies the filter first and then the `SortDescriptions`. The view also rebuilds when an `IObservableList<T>` source raises `Changed`, so a filter set earlier applies at that moment.

If the predicate throws during a rebuild, the exception propagates. The view keeps no items, and `Changed` is not raised for that rebuild.

## Applies to

`CollectionView<T>.Filter`.

## See also

- `Cerneala.UI.Data.CollectionView<T>`
- `Cerneala.UI.Data.SortDescription<T>`
