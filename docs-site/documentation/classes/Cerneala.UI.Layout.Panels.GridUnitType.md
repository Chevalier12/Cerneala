# GridUnitType Enum

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/GridLength.cs`

Specifies how a `GridLength` sizes a grid column or row.

```csharp
public enum GridUnitType
```

## Examples

A 350-pixel-wide grid with a 50-pixel column, a `*` column, and a `2*` column:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout.Panels;

UIRoot root = new(350, 100);
Grid grid = new();
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(50)));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Stars(2)));

UIElement[] cells = new UIElement[3];
for (int column = 0; column < cells.Length; column++)
{
    cells[column] = new UIElement();
    Grid.SetColumn(cells[column], column);
    grid.VisualChildren.Add(cells[column]);
}

root.VisualChildren.Add(grid);
root.ProcessFrame();

Console.WriteLine(string.Join(" ", cells.Select(cell => cell.ArrangedBounds.Width)));
// 50 100 200

Console.WriteLine($"{GridLength.Auto.UnitType} {GridLength.Star.UnitType} {new GridLength(12).UnitType}");
// Auto Star Pixel
```

The two star columns share the 300 pixels left after the 50-pixel column, in the ratio 1 : 2.

## Remarks

`Grid` sizes each definition by its unit type:

| Unit type | Size |
| --- | --- |
| `Pixel` | `Value` pixels. |
| `Auto` | The largest desired size of the children that span only this column or row. Those children are measured with infinite available size on this axis. `Value` is ignored. |
| `Star` | A share of the space left after the `Pixel` and `Auto` definitions: `remaining * Value / (sum of all star values)`. When the available size is infinite, star definitions size to their content instead; at arrange, such a definition gets its weighted share of the final size, but never less than that content size. |

`ColumnDefinition` and `RowDefinition` default to `GridLength.Star`. A grid with no definitions behaves as one star column and one star row. `default(GridLength)` is `Pixel` with `Value` 0.

`GridLength.Validate` throws `ArgumentOutOfRangeException` when the unit type is not one of the three members, or when `Value` is negative, `NaN`, or infinite. `ColumnDefinition` (constructor and `Width` setter), the `RowDefinition.Height` setter, and every grid measure and arrange call it.

In `.crn` markup, the unit type comes from the `Width` attribute of `ColumnDefinition` and the `Height` attribute of `RowDefinition`:

| Attribute | Result |
| --- | --- |
| Missing | `GridLength.Star` |
| `"Auto"` (any case) | `GridLength.Auto` |
| `"*"` | `GridLength.Star` |
| `"2*"` | `GridLength.Stars(2)` |
| `"50"` | `GridLength.Pixels(50)` |

A negative, `NaN`, infinite, or unparsable number is reported as an invalid property value.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Pixel` | `0` | A fixed size of `Value` pixels. |
| `Auto` | `1` | Sizes to the content. |
| `Star` | `2` | A weighted share of the remaining space. |

## Applies to

`GridLength.UnitType`, `ColumnDefinition.Width`, and `RowDefinition.Height`.

## See also

- `Cerneala.UI.Layout.Panels.GridLength`
- `Cerneala.UI.Layout.Panels.Grid`
- `Cerneala.UI.Layout.Panels.ColumnDefinition`
- `Cerneala.UI.Layout.Panels.RowDefinition`
