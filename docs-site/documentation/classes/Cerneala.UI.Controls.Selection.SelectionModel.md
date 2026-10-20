# SelectionModel Class

## Definition
Namespace: `Cerneala.UI.Controls.Selection`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Selection/SelectionModel.cs`

Tracks a set of selected integer indices in selection order and reports selection changes.

```csharp
public class SelectionModel
```

Inheritance:
`object` -> `SelectionModel`

Derived:
`SelectionModel<T>`

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Selection;

SelectionModel model = new();

model.SelectionChanged += (_, args) =>
{
    SelectionChangeResult change = args.Change;
    Console.WriteLine($"Selection changed from {change.OldIndex} to {change.NewIndex}");
};

SelectionChangeResult first = model.Select(2);
bool selected = model.IsSelected(2);

model.Clear();
```

Several indices stay in the order in which they were selected, and `SelectedIndex` is the first of them:

```csharp
using Cerneala.UI.Controls.Selection;

SelectionModel model = new();
model.Select(2);

SelectionChangeResult added = model.Add(0);
// model.SelectedIndices is [2, 0], model.SelectedIndex is 2.
// added is (OldIndex: 2, NewIndex: 2, Changed: true), and SelectionChanged was raised once.

model.Remove(2);
// model.SelectedIndices is [0], model.SelectedIndex is 0.
```

```csharp
using Cerneala.UI.Controls.Selection;

SelectionModel model = new();
model.Select(3);
model.Add(0);

model.SelectRange(3, 1);
// model.SelectedIndices is [3, 1, 2]: 3 was selected and keeps its place, 1 and 2 follow, and 0 is deselected.

model.Select(5);
model.AddRange(0, 1);
// model.SelectedIndices is [5, 0, 1], model.SelectedIndex is 5.
```

## Remarks

`SelectionModel` stores the selected indices in `SelectedIndices`, in selection order and without duplicates. `SelectedIndex` is the first of them, as WPF's `SelectedIndex` is the first item of `SelectedItems`. The initial state is no selection, represented by an empty `SelectedIndices`, `SelectedIndex == -1`, and `HasSelection == false`.

`Select(index)` makes `index` the only selected index. It accepts any value greater than or equal to `-1`; the value `-1` clears the selection, and values less than `-1` throw `ArgumentOutOfRangeException`. For example, with `SelectedIndices` `[1, 3]`, `Select(3)` leaves `[3]` and returns `(1, 3, true)`.

`Add(index)` adds `index` after the selected indices; adding an index that is already selected changes nothing. `Remove(index)` removes `index`; when it was the first selected index, the next one becomes `SelectedIndex`. For example, with `[2, 0]`, `Remove(2)` leaves `[0]` and returns `(2, 0, true)`.

`SelectRange(fromIndex, toIndex)` makes the indices from `fromIndex` to `toIndex`, in either direction, the selection. As WPF's range selection, the selected indices inside the range keep their order, the other indices of the range follow in index order, and the indices outside the range are deselected. `AddRange(fromIndex, toIndex)` adds the indices of the range that are not selected yet, in index order, after the selected indices, and keeps every other selected index. `Add`, `Remove`, `SelectRange`, and `AddRange` throw `ArgumentOutOfRangeException` for a negative index and then leave the selection unchanged.

`IsSelected(index)` returns whether `index` is one of the selected indices. `IsSelected(-1)` is `false`, also when nothing is selected.

Every operation returns a `SelectionChangeResult` with `SelectedIndex` before and after it. When `SelectedIndices` changes, the model updates it, returns a result with `Changed == true`, and raises `SelectionChanged`, also when `SelectedIndex` stays the same: `Add(0)` on `[2]` gives `[2, 0]` and the result `(2, 2, true)`. An operation that leaves `SelectedIndices` the same, such as `Select` of the only selected index or `Add` of a selected index, returns a result with `Changed == false` and does not raise `SelectionChanged`.

`Selector` uses this model internally to keep its `SelectedIndex` property and realized item containers in sync. When the selector's items change and selected items move to other indices, `Selector` moves every selected index in the model, so the model raises `SelectionChanged` even though `Selector` does not raise its routed `SelectionChanged` event for that move. `SelectionModel<T>` derives from this class to add item-based selection over an item list.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionModel()` | Initializes a selection model with no selected index: `SelectedIndices` is empty and `SelectedIndex` is `-1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedIndices` | `IReadOnlyList<int>` | Gets the selected indices in selection order, without duplicates. The list is a read-only view that follows later changes. |
| `HasSelection` | `bool` | Gets whether at least one index is selected. |
| `SelectedIndex` | `int` | Gets the first selected index in selection order, or `-1` when nothing is selected. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Clear()` | `SelectionChangeResult` | Clears the selection by selecting index `-1`. |
| `IsSelected(int index)` | `bool` | Returns `true` when `index` is one of the selected indices; otherwise, `false`. `IsSelected(-1)` is `false`. |
| `Select(int index)` | `SelectionChangeResult` | Makes `index` the only selected index, or clears the selection for `-1`. Throws `ArgumentOutOfRangeException` for values less than `-1`. |
| `Add(int index)` | `SelectionChangeResult` | Adds `index` after the selected indices. Does nothing when `index` is already selected. Throws `ArgumentOutOfRangeException` for a negative `index`. |
| `Remove(int index)` | `SelectionChangeResult` | Removes `index` from the selected indices. Does nothing when `index` is not selected. Throws `ArgumentOutOfRangeException` for a negative `index`. |
| `SelectRange(int fromIndex, int toIndex)` | `SelectionChangeResult` | Makes the indices from `fromIndex` to `toIndex`, in either direction, the selection; selected indices inside the range keep their order. Throws `ArgumentOutOfRangeException` for a negative index. |
| `AddRange(int fromIndex, int toIndex)` | `SelectionChangeResult` | Adds the indices from `fromIndex` to `toIndex`, in either direction, in index order after the selected indices. Throws `ArgumentOutOfRangeException` for a negative index. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>?` | Raised after `SelectedIndices` changes, also when `SelectedIndex` stays the same. The event arguments expose `SelectedIndex` before and after the change through `SelectionChangedEventArgs.Change`. |

## Related Types

| Name | Description |
| --- | --- |
| `SelectionChangeResult` | Immutable result value containing `OldIndex`, `NewIndex`, and `Changed`. |
| `SelectionChangedEventArgs` | Event arguments for `SelectionChanged`; exposes the `SelectionChangeResult` through `Change`. |

## Applies to

Cerneala UI controls that need single or multiple index selection state.

## See also

- `SelectionModel<T>`
- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Controls.ListBox`
