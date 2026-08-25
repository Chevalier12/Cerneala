# DrawPathFactory Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPathFactory.cs`

Creates immutable reusable paths for common compound shapes.

```csharp
public static class DrawPathFactory
```

## Examples

Build geometry once and reuse it across frames.

```csharp
DrawPath badge = DrawPathFactory.Star(
    center: new DrawPoint(48, 48),
    outerRadius: 40,
    innerRadius: 18,
    pointCount: 5,
    rotation: -MathF.PI / 2);

drawing.FillPath(badge, Color.CornflowerBlue, DrawFillRule.EvenOdd);
drawing.DrawPath(badge, outlinePen);
```

## Remarks

All returned paths are immutable and retain open or closed contour state. Angles are radians. `sweepAngle` must be between `0` and `MathF.Tau`; values below or above `MathF.PI` select minor or major arcs, and `MathF.Tau` creates a complete ellipse using two endpoint arcs.

Factory calls allocate new geometry. Cache the returned `DrawPath` when the shape parameters are stable; recording and rendering the same instance preserves its `StableId` and avoids rebuilding geometry.

`Polygon`, `RegularPolygon`, `Star`, `Pie`, `Chord`, and `RoundedRectangle` produce closed contours. `Polyline` and `Arc` produce open contours. Compound shapes share the typed-path tessellation and stroke pipeline rather than defining separate tessellators.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Polygon(IEnumerable<DrawPoint>)` | `DrawPath` | Creates a closed polygon from at least three points. |
| `Polyline(IEnumerable<DrawPoint>)` | `DrawPath` | Creates an open polyline from at least two points. |
| `RoundedRectangle(DrawRect, DrawCornerRadius)` | `DrawPath` | Creates a closed rounded rectangle after proportional radius normalization. |
| `Arc(DrawPoint, float, float, float, float, DrawArcDirection)` | `DrawPath` | Creates an open elliptical arc from center, radii, start angle, sweep magnitude, and direction. |
| `Pie(DrawPoint, float, float, float, float, DrawArcDirection)` | `DrawPath` | Creates a closed sector from the center and arc. |
| `Chord(DrawPoint, float, float, float, float, DrawArcDirection)` | `DrawPath` | Creates a closed arc joined directly from end to start. |
| `RegularPolygon(DrawPoint, float, int, float)` | `DrawPath` | Creates a regular polygon with at least three sides and optional radian rotation. |
| `Star(DrawPoint, float, float, int, float)` | `DrawPath` | Creates an alternating outer/inner-radius star with at least three points. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | The point sequence is null. |
| `ArgumentException` | A polygon or polyline contains too few points. |
| `ArgumentOutOfRangeException` | A radius, angle, sweep, direction, point count, or inner/outer-radius relation is invalid. |

## Applies To

Cerneala typed-path drawing, retained rendering, damage analysis, and Prism scopes.

## See Also

- `DrawPath`
- `DrawPathBuilder`
- `DrawArcDirection`
- `DrawCornerRadius`
- `DrawingContext`
