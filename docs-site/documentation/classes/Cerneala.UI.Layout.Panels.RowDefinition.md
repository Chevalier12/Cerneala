# RowDefinition Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/RowDefinition.cs`

Defines the height behavior for one row in a `Grid`.

```csharp
public sealed class RowDefinition
```

Inheritance:
`object` -> `RowDefinition`

## Examples

Create a grid with fixed, auto, and star-sized rows.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(20)));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Stars(1)));

UIElement child = new();
Grid.SetRow(child, 2);
grid.VisualChildren.Add(child);

grid.Measure(new MeasureContext(new LayoutSize(100, 80)));
grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 80)));
```

Change an existing row height after the definition has been added to a grid.

```csharp
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.RowDefinitions.Add(new RowDefinition());

grid.RowDefinitions[0].Height = GridLength.Pixels(40);
```

## Remarks

`RowDefinition` is used through `Grid.RowDefinitions`. Each definition contributes one row height to the owning grid. If a grid has no row definitions, `Grid` uses a single `GridLength.Star` row internally.

The default constructor creates a definition whose `Height` is `GridLength.Star`. The `RowDefinition(GridLength height)` constructor assigns the supplied value through the `Height` property, so the same validation rules apply during construction and later mutation.

`Height` accepts finite, non-negative `GridLength` values with a valid `GridUnitType`. Invalid values throw `ArgumentOutOfRangeException` from `GridLength.Validate()`.

When a definition is attached to a grid, changing `Height` invalidates the owning grid's measure, arrange, render, and hit-test work. Setting `Height` to the same value does not invalidate the grid.

A `RowDefinition` instance is owned by at most one grid at a time. `GridDefinitionCollection<RowDefinition>` rejects adding the same instance twice to one grid, and the definition's internal attach path rejects sharing the same definition across different grids.

## Constructors

| Name | Description |
| --- | --- |
| `RowDefinition()` | Initializes a row definition with `Height` set to `GridLength.Star`. |
| `RowDefinition(GridLength height)` | Initializes a row definition and sets `Height` to the specified grid length. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Height` | `GridLength` | Gets or sets the height behavior for the row. Defaults to `GridLength.Star`; setting a different value invalidates the owning grid when attached. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RowDefinition(GridLength height)` | `ArgumentOutOfRangeException` | `height` has an invalid unit type, is negative, is `NaN`, or is infinite. |
| `Height` setter | `ArgumentOutOfRangeException` | The assigned `GridLength` has an invalid unit type, is negative, is `NaN`, or is infinite. |

## Applies To

Cerneala retained UI grid layout in the `Cerneala.UI.Layout.Panels` namespace.

## See Also

- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.GridDefinitionCollection<TDefinition>`
- `Cerneala.UI.Layout.Panels.GridLength`
- `Cerneala.UI.Layout.Panels.ColumnDefinition`
