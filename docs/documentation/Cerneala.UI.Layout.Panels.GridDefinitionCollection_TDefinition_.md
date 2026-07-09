# GridDefinitionCollection<TDefinition> Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/GridDefinitionCollection{TDefinition}.cs`

Stores the row or column definitions owned by a `Grid` and invalidates the grid when those definitions change.

```csharp
public sealed class GridDefinitionCollection<TDefinition> :
    IList<TDefinition>,
    IReadOnlyList<TDefinition>
    where TDefinition : class
```

Inheritance:
`object` -> `GridDefinitionCollection<TDefinition>`

Implements:
`IList<TDefinition>`, `ICollection<TDefinition>`, `IEnumerable<TDefinition>`, `IReadOnlyList<TDefinition>`, `IReadOnlyCollection<TDefinition>`, `IEnumerable`

## Type Parameters

| Name | Description |
| --- | --- |
| `TDefinition` | The reference type stored by the collection. `Grid` exposes this as `ColumnDefinition` for columns and `RowDefinition` for rows. |

## Examples

Add and replace grid definitions through the collections exposed by `Grid`.

```csharp
using Cerneala.UI.Layout.Panels;

Grid grid = new();

grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(64)));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(32)));

grid.ColumnDefinitions[0] = new ColumnDefinition(GridLength.Pixels(96));
grid.RowDefinitions.RemoveAt(1);
```

## Remarks

`GridDefinitionCollection<TDefinition>` is the mutable storage behind `Grid.ColumnDefinitions` and `Grid.RowDefinitions`. The collection is created by `Grid`; consumers use the properties on `Grid` rather than constructing this type directly.

Adding, inserting, replacing, removing, or clearing definitions attaches or detaches each definition from the owning grid through the callbacks supplied by `Grid`. Successful mutations call the owner's definition invalidation path, which queues layout, render, and hit-test work for the grid.

The collection rejects `null` definitions. It also rejects adding the same definition instance more than once to the same collection. `ColumnDefinition` and `RowDefinition` add another ownership guard: the same definition instance cannot be shared across different grids.

Setting the indexer to the same object reference is a no-op. Clearing an already empty collection is also a no-op.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of definitions in the collection. |
| `IsReadOnly` | `bool` | Gets `false`; the collection supports mutation. |
| `this[int index]` | `TDefinition` | Gets or replaces the definition at the specified zero-based index. Replacing with a different instance attaches the new definition, detaches the old one, and invalidates the owning grid. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(TDefinition item)` | `void` | Adds a non-null definition to the end of the collection, attaches it to the owner, and invalidates the grid. |
| `Clear()` | `void` | Detaches all definitions, clears the collection, and invalidates the grid when the collection was not already empty. |
| `Contains(TDefinition item)` | `bool` | Returns whether the collection contains `item` using the underlying list comparison. |
| `CopyTo(TDefinition[] array, int arrayIndex)` | `void` | Copies the definitions to `array` starting at `arrayIndex`. |
| `GetEnumerator()` | `IEnumerator<TDefinition>` | Returns an enumerator over the stored definitions. |
| `IndexOf(TDefinition item)` | `int` | Returns the zero-based index of `item`, or `-1` when it is not found. |
| `Insert(int index, TDefinition item)` | `void` | Inserts a non-null definition at `index`, attaches it to the owner, and invalidates the grid. `index` may equal `Count`. |
| `Remove(TDefinition item)` | `bool` | Removes `item` when present, detaches it from the owner, invalidates the grid, and returns whether removal occurred. |
| `RemoveAt(int index)` | `void` | Removes the definition at `index`, detaches it from the owner, and invalidates the grid. |

## Explicit Interface Implementations

| Name | Description |
| --- | --- |
| `IEnumerable.GetEnumerator()` | Returns an untyped enumerator over the stored definitions. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Add(TDefinition)` | `ArgumentNullException` | `item` is `null`. |
| `Add(TDefinition)` | `InvalidOperationException` | The same definition instance is already in the collection, or the definition is already owned by a different grid. |
| `Insert(int, TDefinition)` | `ArgumentNullException` | `item` is `null`. |
| `Insert(int, TDefinition)` | `ArgumentOutOfRangeException` | `index` is less than `0` or greater than `Count`. |
| `Insert(int, TDefinition)` | `InvalidOperationException` | The same definition instance is already in the collection, or the definition is already owned by a different grid. |
| `this[int index]` setter | `ArgumentNullException` | The assigned value is `null`. |
| `this[int index]` setter | `ArgumentOutOfRangeException` | `index` is outside the collection bounds. |
| `this[int index]` setter | `InvalidOperationException` | The assigned definition instance is already in the collection, or the definition is already owned by a different grid. |
| `RemoveAt(int)` | `ArgumentOutOfRangeException` | `index` is outside the collection bounds. |
| `CopyTo(TDefinition[], int)` | `ArgumentNullException` | `array` is `null`. |
| `CopyTo(TDefinition[], int)` | `ArgumentOutOfRangeException` | `arrayIndex` is less than `0`. |
| `CopyTo(TDefinition[], int)` | `ArgumentException` | The destination array does not have enough space from `arrayIndex` to the end. |

## Applies To

Cerneala retained UI grid layout definition collections exposed by `Grid.ColumnDefinitions` and `Grid.RowDefinitions`.

## See Also

- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.ColumnDefinition`
- `Cerneala.UI.Layout.Panels.RowDefinition`
- `Cerneala.UI.Layout.Panels.GridLength`
