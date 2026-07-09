# SelectionChangedEventArgs Class

## Definition

Namespace: `Cerneala.UI.Controls.Selection`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Selection/SelectionChangedEventArgs.cs`

Provides data for the `SelectionModel.SelectionChanged` event.

```csharp
public sealed class SelectionChangedEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `SelectionChangedEventArgs`

## Examples

```csharp
using Cerneala.UI.Controls;

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

## Constructors

| Name | Description |
| --- | --- |
| `SelectionChangedEventArgs(SelectionChangeResult change)` | Initializes the event arguments with the supplied selection change result. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Change` | `SelectionChangeResult` | Gets the selection change result associated with the event. |

## Applies to

`Cerneala` UI controls that use `SelectionModel` for single-index selection state.

## See also

- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.Selection.SelectionChangeResult`
- `Cerneala.UI.Controls.Primitives.Selector`
