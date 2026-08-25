# DrawPathBuilder Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPathBuilder.cs`

Builds immutable typed paths while validating contour state and arc values.

```csharp
public sealed class DrawPathBuilder
```

## Examples

```csharp
DrawPath path = new DrawPathBuilder()
    .MoveTo(new DrawPoint(0, 0))
    .CubicTo(
        new DrawPoint(20, 0),
        new DrawPoint(20, 20),
        new DrawPoint(40, 20))
    .ArcTo(10, 10, 0, isLargeArc: false, sweep: true, new DrawPoint(60, 20))
    .Build();
```

## Remarks

Every contour begins with `MoveTo` and requires at least one drawable segment. After `Close`, call `MoveTo` before adding another segment. `Build` creates a snapshot; later builder changes do not mutate paths already returned.

`ArcTo` uses the SVG endpoint convention. Radii must be positive and finite, and `rotationDegrees` must be finite.

## Constructors

| Name | Description |
| --- | --- |
| `DrawPathBuilder()` | Creates an empty builder with no active contour. |

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `MoveTo(DrawPoint)` | `DrawPathBuilder` | Begins a new contour. |
| `LineTo(DrawPoint)` | `DrawPathBuilder` | Adds a line segment. |
| `QuadraticTo(DrawPoint, DrawPoint)` | `DrawPathBuilder` | Adds a quadratic Bezier control and endpoint. |
| `CubicTo(DrawPoint, DrawPoint, DrawPoint)` | `DrawPathBuilder` | Adds two cubic Bezier controls and an endpoint. |
| `ArcTo(float, float, float, bool, bool, DrawPoint)` | `DrawPathBuilder` | Adds an SVG endpoint-form elliptical arc. |
| `Close()` | `DrawPathBuilder` | Explicitly closes the current contour. |
| `Build()` | `DrawPath` | Returns an immutable snapshot of all contours. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Segment methods | `InvalidOperationException` | No open contour exists. |
| `Close` | `InvalidOperationException` | The contour has no drawable segment. |
| `ArcTo` | `ArgumentOutOfRangeException` | A radius is non-positive/non-finite or rotation is non-finite. |
| `Build` | `InvalidOperationException` | The path or one of its contours has no drawable segment. |

## Applies To

Programmatic reusable path construction.

## See Also

- `DrawPath`
- `DrawPathParser`
