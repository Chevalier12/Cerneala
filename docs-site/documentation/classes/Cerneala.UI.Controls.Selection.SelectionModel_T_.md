# SelectionModel<T> Class

## Definition

Namespace: `Cerneala.UI.Controls.Selection`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Selection/SelectionModel{T}.cs`

Tracks the selected indices and resolves the first of them to a typed selected item from a snapshot of items.

```csharp
public sealed class SelectionModel<T> : SelectionModel
```

Inheritance:
`object` -> `SelectionModel` -> `SelectionModel<T>`

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Selection;

SelectionModel<string> model = new(["one", "two"]);

SelectionChangeResult change = model.SelectItem("two");

string? selected = model.SelectedItem;
int selectedIndex = model.SelectedIndex;
```

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Selection;

SelectionModel<string> model = new(["draft", "review", "published"]);

model.Select(2);
model.SetItems(["draft"]);

bool hasSelection = model.HasSelection; // false
string? selected = model.SelectedItem; // null
```

`SetItems` drops only the selected indices past the new items and keeps the others:

```csharp
using Cerneala.UI.Controls.Selection;

SelectionModel<string> model = new(["a", "b", "c"]);
model.Select(2);
model.Add(0);

model.SetItems(["x", "y"]);

// model.SelectedIndices is [0], and model.SelectedItem is "x".
```

## Remarks

`SelectionModel<T>` extends `SelectionModel` with a typed item snapshot. `SetItems` replaces the snapshot, treats a `null` source as an empty list, and deselects every selected index that is not less than the new item count; the other selected indices stay selected in their order. When that deselects an index, `SelectionChanged` is raised once.

`SelectedItem` is derived from `SelectedIndex`. It returns the item at the selected index only when the index is within the current snapshot; otherwise it returns `default` for `T`.

`SelectItem` searches the snapshot from the first item to the last item and compares values with `EqualityComparer<T>.Default`. If a matching item is found, the model selects only that index. If no matching item is found, the method selects `-1`, which clears the selection.

The base `Select` method allows any index greater than or equal to `-1`. `SelectionModel<T>` does not validate indexes against the current item count when `Select`, `Add`, `SelectRange`, or `AddRange` is called directly.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionModel()` | Initializes an empty typed selection model. |
| `SelectionModel(IEnumerable<T> items)` | Initializes the model and snapshots the supplied items. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedItem` | `T?` | Gets the item at `SelectedIndex` when it is within the current item snapshot; otherwise returns `default`. |
| `SelectedIndex` | `int` | Inherited from `SelectionModel`. Gets the first selected index in selection order. The default is `-1`. |
| `SelectedIndices` | `IReadOnlyList<int>` | Inherited from `SelectionModel`. Gets the selected indices in selection order. |
| `HasSelection` | `bool` | Inherited from `SelectionModel`. Gets whether at least one index is selected. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable<T>? source)` | Replaces the item snapshot. A `null` source becomes an empty snapshot, and every selected index past the new item count is deselected. |
| `SelectItem(T item)` | Selects only the first item equal to `item` by `EqualityComparer<T>.Default`, or clears the selection when the item is not found. |
| `IsSelected(int index)` | Inherited from `SelectionModel`. Returns `true` when `index` is one of the selected indices. |
| `Select(int index)` | Inherited from `SelectionModel`. Makes an index greater than or equal to `-1` the only selected index, returns the change result, and raises `SelectionChanged` when the selected indices change. |
| `Add(int index)`, `Remove(int index)`, `SelectRange(int fromIndex, int toIndex)`, `AddRange(int fromIndex, int toIndex)` | Inherited from `SelectionModel`. Add or remove selected indices; see `SelectionModel`. |
| `Clear()` | Inherited from `SelectionModel`. Clears the selection by selecting `-1`. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>?` | Inherited from `SelectionModel`. Raised when the selected indices change. |

## Applies to

`Cerneala` UI controls.

## See also

- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.Selection.SelectionChangeResult`
- `Cerneala.UI.Controls.Selection.SelectionChangedEventArgs`
