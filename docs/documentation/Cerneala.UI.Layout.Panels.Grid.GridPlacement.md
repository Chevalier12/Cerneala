# Grid.GridPlacement Class

## Definition

Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/Grid.cs`

Stores the per-child row, column, and span values used internally by `Grid`.

```csharp
private sealed class GridPlacement
```

Containing type: `Cerneala.UI.Layout.Panels.Grid`

Inheritance:
`object` -> `Grid.GridPlacement`

## Examples

`GridPlacement` is a private implementation detail. Application code sets and reads placement through the static `Grid` placement methods.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

UIElement child = new();
Grid.SetColumn(child, 1);
Grid.SetRow(child, 0);
Grid.SetColumnSpan(child, 1);
Grid.SetRowSpan(child, 2);

grid.VisualChildren.Add(child);

int column = Grid.GetColumn(child);       // 1
int row = Grid.GetRow(child);             // 0
int columnSpan = Grid.GetColumnSpan(child); // 1
int rowSpan = Grid.GetRowSpan(child);       // 2
```

## Remarks

`GridPlacement` is a private nested storage type used by `Grid` with a `ConditionalWeakTable<UIElement, GridPlacement>`. A placement object is created for an element when one of the static setter methods, such as `Grid.SetColumn` or `Grid.SetRowSpan`, needs to store placement data for that element.

The stored row and column default to `0`. The stored row span and column span default to `1`. When no placement object exists for an element, `Grid.GetColumn` and `Grid.GetRow` return `0`, while `Grid.GetColumnSpan` and `Grid.GetRowSpan` return `1`.

`ClampToAtLeastOneCell` normalizes spans so a placement always covers at least one row and one column. `ClampToGrid` also caps the row and column at the final available index and reduces spans so they do not extend past the final column or row.

The public properties and methods on this type are only reachable from inside the containing `Grid` implementation because `GridPlacement` itself is private. Public application code should use `Grid.GetColumn`, `Grid.SetColumn`, `Grid.GetRow`, `Grid.SetRow`, `Grid.GetColumnSpan`, `Grid.SetColumnSpan`, `Grid.GetRowSpan`, and `Grid.SetRowSpan`.

## Constructors

| Name | Description |
| --- | --- |
| `GridPlacement()` | Initializes a placement record with `Row` and `Column` set to `0`, and `RowSpan` and `ColumnSpan` set to `1`. The constructor is implicit and not directly accessible outside the private nested type. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Row` | `int` | Gets or sets the stored row index for the child element. Defaults to `0`. |
| `Column` | `int` | Gets or sets the stored column index for the child element. Defaults to `0`. |
| `RowSpan` | `int` | Gets or sets the stored number of rows covered by the child element. Defaults to `1`. |
| `ColumnSpan` | `int` | Gets or sets the stored number of columns covered by the child element. Defaults to `1`. |

## Methods

| Name | Description |
| --- | --- |
| `ClampToAtLeastOneCell()` | Sets `RowSpan` and `ColumnSpan` to at least `1`. |
| `ClampToGrid(int columnCount, int rowCount)` | Treats the grid as having at least one column and one row, caps `Column` and `Row` at the final available index, and reduces spans so they remain inside the grid from the current `Column` and `Row`. |

## Applies To

Internal child placement storage for `Cerneala.UI.Layout.Panels.Grid`.

## See Also

- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.ColumnDefinition`
- `Cerneala.UI.Layout.Panels.RowDefinition`
- `Cerneala.UI.Elements.UIElement`
