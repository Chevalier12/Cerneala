# Polygon Class

## Definition

Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Polygon.cs`

Draws a closed contour of connected straight segments.

```csharp
public sealed class Polygon : Shape
```

Inheritance: `Object` → `UiObject` → `UIElement` → `Control` → `Shape` → `Polygon`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Polygon triangle = new()
{
    Points = [new DrawPoint(40, 0), new DrawPoint(80, 60), new DrawPoint(0, 60)],
    Fill = new SolidColorBrush(Color.White),
    Stroke = new SolidColorBrush(Color.Black)
};
```

```xml
<Polygon Points="40,0 80,60 0,60" Fill="White" Stroke="Black" FillRule="EvenOdd" />
```

## Remarks

The stroke closes from the final point back to the first. At least three points are required; fewer points produce no geometry. Fill and stroke share the same immutable path. The inherited `FillRule` defaults to `NonZero` and can be set to `EvenOdd`.

`Points` defaults to an empty list. Every assignment, including `SetValue`, makes a read-only copy. Later mutation of the input collection cannot modify retained geometry. Replace `Points` to update the polygon; in-place collection edits and collection-change notifications are not supported. `null` is rejected.

Coordinates are local to the arranged position and are not automatically stretched. Point changes affect measure and render; unchanged snapshots reuse geometry. An explicit inherited `Geometry` takes precedence over `Points`.

The `.crn` point literal format is the same as [Polyline](Cerneala.UI.Controls.Shapes.Polyline.md): finite invariant-culture X/Y pairs separated by commas or whitespace.

## Constructors

| Name | Description |
| --- | --- |
| `Polygon()` | Initializes an empty polygon. |

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
| `ResolveGeometry(LayoutRect)` | Protected override resolving explicit geometry or the cached closed contour. |

## See Also

- [Shape](Cerneala.UI.Controls.Shapes.Shape.md)
- [Polyline](Cerneala.UI.Controls.Shapes.Polyline.md)
- [Path](Cerneala.UI.Controls.Shapes.Path.md)
