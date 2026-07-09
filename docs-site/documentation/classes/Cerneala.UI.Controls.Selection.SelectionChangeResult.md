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

SelectionModel model = new();

SelectionChangeResult first = model.Select(2);
Console.WriteLine(first.OldIndex); // -1
Console.WriteLine(first.NewIndex); // 2
Console.WriteLine(first.Changed); // true

SelectionChangeResult second = model.Select(2);
Console.WriteLine(second.Changed); // false
```

## Remarks

`SelectionChangeResult` is returned by `SelectionModel.Select`, `SelectionModel.Clear`, and `SelectionModel<T>.SelectItem`. It is also exposed through `SelectionChangedEventArgs.Change` when `SelectionModel.SelectionChanged` is raised.

`OldIndex` is the selected index before the operation, and `NewIndex` is the selected index requested by the operation. `Changed` is `true` when the selected index actually changed. Selecting the same index again returns a result with `Changed == false`.

The value `-1` represents no selection in `SelectionModel`. The type is a readonly record struct, so it has value equality and compiler-synthesized record members such as `Deconstruct`, `Equals`, `GetHashCode`, and `ToString`.

## Constructors

| Name | Description |
| --- | --- |
| `SelectionChangeResult(int OldIndex, int NewIndex, bool Changed)` | Initializes a selection change result with the previous index, requested new index, and change flag. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OldIndex` | `int` | Gets the selected index before the selection operation. |
| `NewIndex` | `int` | Gets the selected index requested by the selection operation. |
| `Changed` | `bool` | Gets whether the operation changed the selected index. |

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

Cerneala UI controls that use `SelectionModel` for single-index selection state.

## See also

- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.Selection.SelectionModel<T>`
- `Cerneala.UI.Controls.Selection.SelectionChangedEventArgs`
