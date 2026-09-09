# Polyline Class

## Definition

Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Polyline.cs`

Draws an open sequence of connected straight segments.

```csharp
public sealed class Polyline : Shape
```

Inheritance: `Object` → `UiObject` → `UIElement` → `Control` → `Shape` → `Polyline`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Polyline line = new()
{
    Points = [new DrawPoint(0, 0), new DrawPoint(40, 20), new DrawPoint(80, 0)],
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

```xml
<Polyline Points="0,0 40,20 80,0" Stroke="Black" StrokeThickness="2" />
```

## Remarks

`Points` defaults to an empty list. Assigning it, including through `SetValue`, copies the sequence into a read-only snapshot. Mutating the original array or list does not change the shape. Replace `Points` to update the geometry; in-place collection editing and collection-change notifications are not supported. `null` is rejected.

At least two points are needed to produce geometry. All segments share one native stroke path, preserving joins. The last point is not connected back to the first by the stroke. A fill, when set, follows the drawing fill contract: open contours are implicitly closed for filling only. `FillRule` selects the fill rule.

Coordinates are local to the arranged position, without automatic stretching. Point changes affect measure and render. Unchanged point snapshots reuse the immutable geometry. An explicit inherited `Geometry` takes precedence over `Points`.

In `.crn`, `Points` accepts finite invariant-culture coordinate pairs separated by commas or whitespace. An empty value produces an empty list; an odd coordinate count or non-finite coordinate is invalid.

## Constructors

| Name | Description |
| --- | --- |
| `Polyline()` | Initializes an empty polyline. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `PointsProperty` | `UiProperty<IReadOnlyList<DrawPoint>>` | Identifies the snapshotted point list; affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<DrawPoint>` | Gets the immutable point snapshot or replaces it with a copied sequence. |

## Methods

| Name | Description |
| --- | --- |
| `ResolveGeometry(LayoutRect)` | Protected override resolving explicit geometry or the cached open contour. |

## See Also

- [Shape](Cerneala.UI.Controls.Shapes.Shape.md)
- [Polygon](Cerneala.UI.Controls.Shapes.Polygon.md)
- [Line](Cerneala.UI.Controls.Shapes.Line.md)
