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

During measure, pixel definitions use their fixed size. Auto definitions are sized from the visible children they hold. Star definitions receive a proportional share of the remaining finite available size. When the available size for an axis is positive infinity, star shares are not resolved; star definitions on that axis are then sized from the visible children they hold, like `Auto` definitions.

A child sizes the definitions it spans only when all of them are content-sized: `Auto` definitions, and star definitions on an axis measured with positive infinity. Children that occupy exactly one such definition size it first: it grows to the largest desired size among them. Then each child that spans several such definitions adds only the size those definitions still miss, narrowest span first, and the missing size is shared equally, as in WPF. When the span holds star definitions, only those star definitions grow and its `Auto` definitions keep their size, as in WPF. For example, with two `Auto` columns, a child with a desired width of 100 and a column span of 2 makes the columns 50 and 50 pixels wide, and the grid's desired width is 100. If another child with a desired width of 80 is in the first column only, the columns are 90 and 10 pixels wide. A child whose span includes a pixel definition, or a star definition on an axis with a finite available size, sizes none of the definitions it spans. Pixel definitions always keep their size; WPF grows the pixel definition in that case instead. For example, with an `Auto` column and a 100-pixel column, a child with a column span of 2 is measured with an available width of 100, and the `Auto` column stays 0 pixels wide.

A child whose columns are all `Auto` is measured with positive infinity as its available width, and a child whose rows are all `Auto` is measured with positive infinity as its available height, as in WPF. The `Auto` definition therefore sizes to the child's content, not to the space the grid received. On every other axis, a child is finally measured with the combined resolved size of the columns or rows it spans. For example, an `Auto` row that holds a `Border` wrapping a `Grid` with a 20-pixel-high child is 20 pixels high plus the border insets, and the `*` rows after it share the rest of the grid height.

During arrange, pixel sizes are resolved again, auto sizes use the sizes captured during measure, and star sizes are resolved from the final arranged size. After a measure with a finite available size on an axis, the star definitions on that axis share the remaining final size only by weight. After a measure with positive infinity on an axis, the content size each star definition received during measure is its minimum, as in WPF: a star definition whose weighted share is smaller than its minimum keeps its minimum, and the other star definitions share the space that is left by weight. When the minimums do not fit, every star definition keeps its minimum and the rows or columns extend past the final size. For example, a grid with two `*` rows holding a 10-pixel-high child and a 30-pixel-high child, measured with positive infinity as its available height (as a vertical `StackPanel` or an `Auto` row measures it), has a desired height of 40. Arranged at 40 its rows are 10 and 30 pixels high, arranged at 50 they are 20 and 30, arranged at 100 they are 50 and 50, and arranged at 30 they stay 10 and 30. A child is arranged at the origin of its resolved row and column and receives the combined size of the rows and columns it spans.

When the `ArrangeContext` has layout rounding enabled (`LayoutManager` always passes `LayoutRounding.ForScale(root.Scale)`), the resolved column widths and row heights are rounded to whole device pixels before the children are arranged, as in WPF. Each size is rounded to the nearest device pixel. When the rounded sizes do not add up to the rounded total, the sizes that rounding moved furthest change by one device pixel each until they do: the sizes rounded down the most grow first, and the sizes rounded up the most shrink first. When two sizes moved equally far, the earlier definition grows first and the later definition shrinks first. Row and column offsets are sums of the rounded sizes, added to the grid's origin rounded to the nearest device pixel, which is where the grid's own rounded bounds start (`UIElement.Arrange` passes `ArrangeCore` the grid's rounded bounds). Neighbouring cells therefore meet exactly and the last cell ends on the grid's edge, also when the grid's final rectangle starts between device pixels. For example, three `*` columns in a 100-pixel-wide grid at scale 1 are 34, 33, and 33 pixels wide, at x = 0, 34, and 67. A grid with `Width="100"` and `HorizontalAlignment="Center"` in a 101-pixel-wide slot starts at x = 0.5, which rounds to 0, so its columns are also at x = 0, 34, and 67 and the last column ends at 100. At scale 1.25 the same grid is 125 device pixels wide, and the columns are 42, 42, and 41 device pixels wide.

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
