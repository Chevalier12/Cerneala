# DrawPath Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPath.cs`

Represents immutable, reusable typed 2D path geometry.

```csharp
public sealed class DrawPath
```

## Examples

```csharp
DrawPath path = new DrawPathBuilder()
    .MoveTo(new DrawPoint(0, 0))
    .LineTo(new DrawPoint(40, 0))
    .LineTo(new DrawPoint(20, 32))
    .Close()
    .Build();

drawing.FillPath(path, Color.Tomato);
drawing.DrawPath(path, new DrawPen(outlineBrush, 2));
```

## Remarks

A path snapshots its contours and segments at build time. Reusing the same instance preserves `StableId`, avoids reparsing SVG, and gives retained rendering and tessellation caches a stable geometry identity. `Bounds` conservatively includes line, curve-control, and elliptical-arc extents.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Contours` | `IReadOnlyList<DrawPathContour>` | Gets the immutable open and closed contours. |
| `Bounds` | `DrawRect` | Gets conservative local geometry bounds. |
| `StableId` | `long` | Gets the identity assigned to this immutable path instance. |

## Applies To

Path fill, stroke, clipping, retained rendering, and Prism capture.

## See Also

- `DrawPathBuilder`
- `DrawPathParser`
- `DrawFillRule`
- `DrawPen`
