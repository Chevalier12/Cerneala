# SelectionModel<T> Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SelectionModel{T}.cs`

Tracks a single selected index and resolves that index to a typed selected item from a snapshot of items.

```csharp
public sealed class SelectionModel<T> : SelectionModel
```

Inheritance:
`object` -> `SelectionModel` -> `SelectionModel<T>`

## Examples

```csharp
using Cerneala.UI.Controls;

SelectionModel<string> model = new(["one", "two"]);

SelectionChangeResult change = model.SelectItem("two");

string? selected = model.SelectedItem;
int selectedIndex = model.SelectedIndex;
```

```csharp
using Cerneala.UI.Controls;

SelectionModel<string> model = new(["draft", "review", "published"]);

model.Select(2);
model.SetItems(["draft"]);

bool hasSelection = model.HasSelection; // false
string? selected = model.SelectedItem; // null
```

## Remarks

`SelectionModel<T>` extends `SelectionModel` with a typed item snapshot. `SetItems` replaces the snapshot, treats a `null` source as an empty list, and clears the current selection when the selected index is outside the new item count.

`SelectedItem` is derived from `SelectedIndex`. It returns the item at the selected index only when the index is within the current snapshot; otherwise it returns `default` for `T`.

`SelectItem` searches the snapshot from the first item to the last item and compares values with `EqualityComparer<T>.Default`. If a matching item is found, the model selects that index. If no matching item is found, the method selects `-1`, which clears the selection.

The base `Select` method allows any index greater than or equal to `-1`. `SelectionModel<T>` does not validate indexes against the current item count when `Select` is called directly.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionModel()` | Initializes an empty typed selection model. |
| `SelectionModel(IEnumerable<T> items)` | Initializes the model and snapshots the supplied items. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedItem` | `T?` | Gets the selected item when `SelectedIndex` is within the current item snapshot; otherwise returns `default`. |
| `SelectedIndex` | `int` | Inherited from `SelectionModel`. Gets the current selected index. The default is `-1`. |
| `HasSelection` | `bool` | Inherited from `SelectionModel`. Gets whether `SelectedIndex` is greater than or equal to zero. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable<T>? source)` | Replaces the item snapshot. A `null` source becomes an empty snapshot, and an out-of-range selected index is cleared. |
| `SelectItem(T item)` | Selects the first item equal to `item` by `EqualityComparer<T>.Default`, or clears the selection when the item is not found. |
| `IsSelected(int index)` | Inherited from `SelectionModel`. Returns `true` when `index` equals the current selected index. |
| `Select(int index)` | Inherited from `SelectionModel`. Selects an index greater than or equal to `-1`, returns the change result, and raises `SelectionChanged` when the index changes. |
| `Clear()` | Inherited from `SelectionModel`. Clears the selection by selecting `-1`. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>?` | Inherited from `SelectionModel`. Raised when the selected index changes. |

## Applies to

`Cerneala` UI controls.

## See also

- `Cerneala.UI.Controls.SelectionModel`
- `Cerneala.UI.Controls.SelectionChangeResult`
- `Cerneala.UI.Controls.SelectionChangedEventArgs`
