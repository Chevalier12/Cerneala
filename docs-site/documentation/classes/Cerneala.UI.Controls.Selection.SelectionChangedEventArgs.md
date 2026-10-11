# SelectionChangedEventArgs Class

## Definition

Namespace: `Cerneala.UI.Controls.Selection`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Selection/SelectionChangedEventArgs.cs`

Provides data for the `SelectionModel.SelectionChanged` and `Selector.SelectionChanged` events.

```csharp
public sealed class SelectionChangedEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `SelectionChangedEventArgs`

## Examples

```csharp
using Cerneala.UI.Controls.Selection;

SelectionModel model = new();

model.SelectionChanged += (_, args) =>
{
    SelectionChangeResult change = args.Change;
    Console.WriteLine($"Selection changed from {change.OldIndex} to {change.NewIndex}");
};

model.Select(3);
model.Clear();
```

## Remarks

`SelectionChangedEventArgs` wraps the `SelectionChangeResult` produced by `SelectionModel.Select(int)`.

The `Change` property contains the previous selected index, the new selected index, and the `Changed` flag. `SelectionModel` raises `SelectionChanged` only after the stored selected index changes; selecting the current index again returns a result with `Changed == false` and does not raise the event.

The value `-1` represents no selection in `SelectionModel`, so a change whose `NewIndex` is `-1` represents a cleared selection.

The same type is used in two ways:

- `SelectionModel.SelectionChanged` is a plain .NET event. It uses the one-argument constructor, so only `Change` is set. `RemovedItems` and `AddedItems` are empty, and `RoutedEvent`, `Source` and `OriginalSource` are not set.
- `Selector.SelectionChanged` is the bubbling routed event `Selector.SelectionChangedEvent`. `Selector` raises it after it handles the model change, with the selector as `Source`. `RemovedItems` holds the item at `Change.OldIndex` and `AddedItems` holds the item at `Change.NewIndex`, each only when that index is inside the current item range; otherwise the list is empty. For example, selecting index `1` of a `ListBox` with items `"A"` and `"B"` when nothing was selected gives `RemovedItems = []` and `AddedItems = ["B"]`.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionChangedEventArgs(SelectionChangeResult change)` | Initializes the event arguments with the supplied selection change result. `RemovedItems` and `AddedItems` are empty. |
| `SelectionChangedEventArgs(RoutedEvent routedEvent, object source, SelectionChangeResult change, IReadOnlyList<object?> removedItems, IReadOnlyList<object?> addedItems)` | Initializes routed event arguments with the change and the removed and added items. Throws `ArgumentNullException` when `routedEvent` or `source` is `null`. `Selector` uses this constructor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Change` | `SelectionChangeResult` | Gets the selection change result associated with the event. |
| `RemovedItems` | `IReadOnlyList<object?>` | Gets the items that left the selection. Empty when the one-argument constructor was used. |
| `AddedItems` | `IReadOnlyList<object?>` | Gets the items that entered the selection. Empty when the one-argument constructor was used. |

`RoutedEvent`, `Source`, `OriginalSource` and `Handled` are inherited from `RoutedEventArgs`.

## Applies to

`Cerneala` UI controls that use `SelectionModel` for single-index selection state.

## See also

- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.Selection.SelectionChangeResult`
- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Input.RoutedEventArgs`
