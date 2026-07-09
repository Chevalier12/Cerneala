# ColumnDefinition Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/ColumnDefinition.cs`

Defines the width behavior for one column in a `Grid`.

```csharp
public sealed class ColumnDefinition
```

Inheritance:
`object` -> `ColumnDefinition`

## Examples

Create a grid with fixed, auto, and star-sized columns.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(20)));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(1)));

UIElement child = new();
Grid.SetColumn(child, 2);
grid.VisualChildren.Add(child);

grid.Measure(new MeasureContext(new LayoutSize(100, 50)));
grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 50)));
```

Change an existing column width after the definition has been added to a grid.

```csharp
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.ColumnDefinitions.Add(new ColumnDefinition());

grid.ColumnDefinitions[0].Width = GridLength.Pixels(40);
```

## Remarks

`ColumnDefinition` is used through `Grid.ColumnDefinitions`. Each definition contributes one column width to the owning grid. If a grid has no column definitions, `Grid` uses a single `GridLength.Star` column internally.

The default constructor creates a definition whose `Width` is `GridLength.Star`. The `ColumnDefinition(GridLength width)` constructor assigns the supplied value through the `Width` property, so the same validation rules apply during construction and later mutation.

`Width` accepts finite, non-negative `GridLength` values with a valid `GridUnitType`. Invalid values throw `ArgumentOutOfRangeException` from `GridLength.Validate()`.

When a definition is attached to a grid, changing `Width` invalidates the owning grid's measure, arrange, render, and hit-test work. Setting `Width` to the same value does not invalidate the grid.

A `ColumnDefinition` instance is owned by at most one grid at a time. `GridDefinitionCollection<ColumnDefinition>` rejects adding the same instance twice to one grid, and the definition's internal attach path rejects sharing the same definition across different grids.

## Constructors

| Name | Description |
| --- | --- |
| `ColumnDefinition()` | Initializes a column definition with `Width` set to `GridLength.Star`. |
| `ColumnDefinition(GridLength width)` | Initializes a column definition and sets `Width` to the specified grid length. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Width` | `GridLength` | Gets or sets the width behavior for the column. Defaults to `GridLength.Star`; setting a different value invalidates the owning grid when attached. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ColumnDefinition(GridLength width)` | `ArgumentOutOfRangeException` | `width` has an invalid unit type, is negative, is `NaN`, or is infinite. |
| `Width` setter | `ArgumentOutOfRangeException` | The assigned `GridLength` has an invalid unit type, is negative, is `NaN`, or is infinite. |

## Applies To

Cerneala retained UI grid layout in the `Cerneala.UI.Layout.Panels` namespace.

## See Also

- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.GridDefinitionCollection<TDefinition>`
- `Cerneala.UI.Layout.Panels.GridLength`
- `Cerneala.UI.Layout.Panels.RowDefinition`
