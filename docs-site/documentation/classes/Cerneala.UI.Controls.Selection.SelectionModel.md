# SelectionModel Class

## Definition
Namespace: `Cerneala.UI.Controls.Selection`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Selection/SelectionModel.cs`

Tracks a single selected integer index and reports selection changes.

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

## Remarks

`SelectionModel` stores one selected index at a time. The initial state is no selection, represented by `SelectedIndex == -1` and `HasSelection == false`.

Calling `Select(index)` accepts any value greater than or equal to `-1`. The value `-1` clears selection. Values less than `-1` throw `ArgumentOutOfRangeException`.

When the selected index changes, `SelectionModel` updates `SelectedIndex`, returns a `SelectionChangeResult` with `Changed == true`, and raises `SelectionChanged`. Selecting the current index again returns a result with `Changed == false` and does not raise `SelectionChanged`.

`Selector` uses this model internally to keep its `SelectedIndex` property and realized item containers in sync. `SelectionModel<T>` derives from this class to add item-based selection over an item list.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionModel()` | Initializes a selection model with `SelectedIndex` set to `-1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasSelection` | `bool` | Gets whether `SelectedIndex` is greater than or equal to `0`. |
| `SelectedIndex` | `int` | Gets the currently selected index, or `-1` when nothing is selected. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Clear()` | `SelectionChangeResult` | Clears selection by selecting index `-1`. |
| `IsSelected(int index)` | `bool` | Returns `true` when `index` equals the current `SelectedIndex`; otherwise, `false`. |
| `Select(int index)` | `SelectionChangeResult` | Selects `index`, raises `SelectionChanged` when the value changes, and throws `ArgumentOutOfRangeException` for values less than `-1`. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>?` | Raised after `SelectedIndex` changes. The event arguments expose the old index, new index, and change flag through `SelectionChangedEventArgs.Change`. |

## Related Types

| Name | Description |
| --- | --- |
| `SelectionChangeResult` | Immutable result value containing `OldIndex`, `NewIndex`, and `Changed`. |
| `SelectionChangedEventArgs` | Event arguments for `SelectionChanged`; exposes the `SelectionChangeResult` through `Change`. |

## Applies to

Cerneala UI controls that need single-index selection state.

## See also

- `SelectionModel<T>`
- `Cerneala.UI.Controls.Primitives.Selector`
