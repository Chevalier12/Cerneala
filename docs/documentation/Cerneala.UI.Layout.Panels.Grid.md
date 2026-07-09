# Grid Class

## Definition
Namespace: `Cerneala.UI.Layout.Panels`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Panels/Grid.cs`

Arranges visual children into rows and columns using pixel, auto, and star-sized grid definitions.

```csharp
public class Grid : Panel
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Panel` -> `Grid`

## Examples

Create a two-column grid with two rows, place children into cells, and span one child across both columns.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

Grid grid = new();
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Pixels(80)));
grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
grid.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(24)));

FixedElement header = new(new LayoutSize(120, 18));
FixedElement left = new(new LayoutSize(40, 24));
FixedElement content = new(new LayoutSize(160, 24));

Grid.SetColumn(header, 0);
Grid.SetColumnSpan(header, 2);
Grid.SetRow(header, 0);

Grid.SetColumn(left, 0);
Grid.SetRow(left, 1);

Grid.SetColumn(content, 1);
Grid.SetRow(content, 1);

grid.VisualChildren.Add(header);
grid.VisualChildren.Add(left);
grid.VisualChildren.Add(content);

grid.Measure(new MeasureContext(new LayoutSize(300, 100)));
grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 100)));

internal sealed class FixedElement(LayoutSize size) : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return size;
    }
}
```

## Remarks

`Grid` is a layout panel for row and column based positioning. Child placement is stored per `UIElement` through the static `Get*` and `Set*` methods on `Grid`. Unset placement uses column `0`, row `0`, column span `1`, and row span `1`.

If `ColumnDefinitions` is empty, the grid behaves as if it has one star-sized column. If `RowDefinitions` is empty, it behaves as if it has one star-sized row.

During measure, pixel definitions use their fixed size. Auto definitions are sized from visible children that occupy exactly one column or exactly one row. Star definitions receive a proportional share of the remaining finite available size. When the available size for an axis is positive infinity, star sizes are not resolved during that measure pass.

During arrange, pixel sizes are resolved again, auto sizes use the sizes captured during measure, and star sizes are resolved from the final arranged size. A child is arranged at the origin of its resolved row and column and receives the combined size of the rows and columns it spans.

Collapsed children are measured with `LayoutSize.Zero` and arranged at the grid final rectangle origin with zero size. Placement is clamped to the available grid bounds during layout: indices beyond the last row or column are treated as the last available cell, and spans are capped so they do not extend beyond the grid.

Changing a child placement invalidates measure and arrange on the parent when the element is currently parented by a `Grid` and the stored placement actually changes. Mutating row or column definitions invalidates measure, arrange, render, and hit testing for the owning grid.

## Constructors

| Name | Description |
| --- | --- |
| `Grid()` | Initializes a new grid panel with empty row and column definition collections. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ColumnDefinitions` | `GridDefinitionCollection<ColumnDefinition>` | Gets the mutable collection of column definitions used to resolve horizontal cell sizes. |
| `RowDefinitions` | `GridDefinitionCollection<RowDefinition>` | Gets the mutable collection of row definitions used to resolve vertical cell sizes. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetColumn(UIElement element)` | `int` | Gets the stored column index for `element`, or `0` when no placement is stored. Throws `ArgumentNullException` when `element` is `null`. |
| `SetColumn(UIElement element, int column)` | `void` | Sets the stored column index for `element`. `column` must be non-negative. |
| `GetRow(UIElement element)` | `int` | Gets the stored row index for `element`, or `0` when no placement is stored. Throws `ArgumentNullException` when `element` is `null`. |
| `SetRow(UIElement element, int row)` | `void` | Sets the stored row index for `element`. `row` must be non-negative. |
| `GetColumnSpan(UIElement element)` | `int` | Gets the stored column span for `element`, or `1` when no placement is stored. Throws `ArgumentNullException` when `element` is `null`. |
| `SetColumnSpan(UIElement element, int columnSpan)` | `void` | Sets the stored column span for `element`. `columnSpan` must be positive. |
| `GetRowSpan(UIElement element)` | `int` | Gets the stored row span for `element`, or `1` when no placement is stored. Throws `ArgumentNullException` when `element` is `null`. |
| `SetRowSpan(UIElement element, int rowSpan)` | `void` | Sets the stored row span for `element`. `rowSpan` must be positive. |

## Attached Placement Defaults

| Placement Value | Default |
| --- | --- |
| Column | `0` |
| Row | `0` |
| Column span | `1` |
| Row span | `1` |

## Definition Collections

| Collection | Definition Type | Size Property | Default Definition Size |
| --- | --- | --- | --- |
| `ColumnDefinitions` | `ColumnDefinition` | `Width` | `GridLength.Star` |
| `RowDefinitions` | `RowDefinition` | `Height` | `GridLength.Star` |

Definitions validate their `GridLength` values when assigned. A definition cannot be shared across grids, and the same definition instance cannot be added to the same definition collection more than once.

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `VisualChildren` | `UIElementCollection` | Gets the visual child collection inherited from `UIElement`; `Grid` measures and arranges these children into resolved cells. |
| `DesiredSize` | `LayoutSize` | Gets the size produced by the last measure pass. |
| `ArrangedBounds` | `LayoutRect` | Gets the bounds produced by the last arrange pass. |

## Applies to

`Cerneala` retained UI layout panels that need row and column based child layout.

## See also

- `Cerneala.UI.Layout.Panels.Panel`
- `Cerneala.UI.Layout.Panels.ColumnDefinition`
- `Cerneala.UI.Layout.Panels.RowDefinition`
- `Cerneala.UI.Layout.Panels.GridLength`
- `Cerneala.UI.Elements.UIElement`
