# SelectionChangeResult Struct

## Definition

Namespace: `Cerneala.UI.Controls.Selection`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Selection/SelectionChangeResult.cs`

Represents the old index, new index, and change state produced by a selection operation.

```csharp
public readonly record struct SelectionChangeResult(int OldIndex, int NewIndex, bool Changed);
```

Inheritance:
`object` -> `ValueType` -> `SelectionChangeResult`

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Selection;

SelectionModel model = new();

SelectionChangeResult first = model.Select(2);
Console.WriteLine(first.OldIndex); // -1
Console.WriteLine(first.NewIndex); // 2
Console.WriteLine(first.Changed); // true

SelectionChangeResult second = model.Select(2);
Console.WriteLine(second.Changed); // false
```

## Remarks

`SelectionChangeResult` is returned by `SelectionModel.Select`, `Add`, `Remove`, `SelectRange`, `AddRange`, `Clear`, and `SelectionModel<T>.SelectItem`. It is also exposed through `SelectionChangedEventArgs.Change` when `SelectionModel.SelectionChanged` is raised.

`OldIndex` is `SelectionModel.SelectedIndex` before the operation, and `NewIndex` is `SelectionModel.SelectedIndex` after it. `Changed` is `true` when the operation changed the selected indices, also when `SelectedIndex` stays the same. For example, `Add(0)` on a model whose selected indices are `[2]` returns `(OldIndex: 2, NewIndex: 2, Changed: true)`, and `Remove(2)` on `[2, 0]` returns `(2, 0, true)`. Selecting the only selected index again returns a result with `Changed == false`.

The value `-1` represents no selection in `SelectionModel`. The type is a readonly record struct, so it has value equality and compiler-synthesized record members such as `Deconstruct`, `Equals`, `GetHashCode`, and `ToString`.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionChangeResult(int OldIndex, int NewIndex, bool Changed)` | Initializes a selection change result with the previous index, new index, and change flag. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OldIndex` | `int` | Gets `SelectedIndex` before the selection operation. |
| `NewIndex` | `int` | Gets `SelectedIndex` after the selection operation. |
| `Changed` | `bool` | Gets whether the operation changed the selected indices. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out int OldIndex, out int NewIndex, out bool Changed)` | `void` | Deconstructs the result into its old index, new index, and change flag. |
| `Equals(object? obj)` | `bool` | Returns whether `obj` is an equal `SelectionChangeResult`. |
| `Equals(SelectionChangeResult other)` | `bool` | Returns whether another result has the same `OldIndex`, `NewIndex`, and `Changed` values. |
| `GetHashCode()` | `int` | Returns a hash code derived from the record values. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Operators

| Name | Return Type | Description |
| --- | --- | --- |
| `operator ==(SelectionChangeResult left, SelectionChangeResult right)` | `bool` | Returns whether two results are equal by value. |
| `operator !=(SelectionChangeResult left, SelectionChangeResult right)` | `bool` | Returns whether two results are not equal by value. |

## Applies to

Cerneala UI controls that use `SelectionModel` for their selection state.

## See also

- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.Selection.SelectionModel<T>`
- `Cerneala.UI.Controls.Selection.SelectionChangedEventArgs`
