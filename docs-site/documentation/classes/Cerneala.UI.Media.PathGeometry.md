# PathGeometry Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/PathGeometry.cs`

Represents an immutable path geometry made from one or more drawing points.

```csharp
public sealed record PathGeometry : Geometry
```

Inheritance:
`object` -> `Geometry` -> `PathGeometry`

Implements:
`IEquatable<PathGeometry>`

## Examples

Create a path geometry from connected points and read its calculated bounds:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

PathGeometry geometry = new(
[
    new DrawPoint(10, 20),
    new DrawPoint(50, 20),
    new DrawPoint(50, 60)
]);

DrawRect bounds = geometry.Bounds; // X = 10, Y = 20, Width = 40, Height = 40
IReadOnlyList<DrawPoint> points = geometry.Points;
```

Use a `PathGeometry` as the data for a path shape:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;
using PathShape = Cerneala.UI.Controls.Shapes.Path;

PathShape path = new()
{
    Data = new PathGeometry(
    [
        new DrawPoint(0, 0),
        new DrawPoint(40, 0),
        new DrawPoint(40, 24)
    ]),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

## Remarks

`PathGeometry` stores the supplied `IEnumerable<DrawPoint>` as a read-only point list. The constructor copies the sequence into an array before exposing it through `Points`, so later changes to the original collection do not change the geometry.

At least one point is required. Passing `null` throws `ArgumentNullException`; passing an empty sequence throws `ArgumentException`.

`Bounds` is calculated from the minimum and maximum X and Y coordinates in `Points`. A geometry with one point has a zero-width, zero-height bounds rectangle at that point.

When rendered through `Shape`, `PathGeometry` is drawn as connected line segments between consecutive points. The renderer uses `Stroke`, `StrokeThickness`, `Opacity`, and `RenderTransform`; `Fill` is not used for `PathGeometry` rendering. A path with one point has no segment to draw, although it still has valid bounds.

Because `PathGeometry` is a sealed record, instances use record equality. The `Points` property is a read-only wrapper around the copied array, so equality compares that wrapper reference rather than performing point-by-point sequence equality.

## Constructors

| Name | Description |
| --- | --- |
| `PathGeometry(IEnumerable<DrawPoint> points)` | Initializes a path geometry from one or more points and calculates its bounds. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<DrawPoint>` | Gets the copied, read-only point list that defines the path. |
| `Bounds` | `DrawRect` | Gets the bounding rectangle calculated from the minimum and maximum point coordinates. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(PathGeometry?)` | Determines whether another `PathGeometry` has equal record state. |
| `Equals(object?)` | Determines whether an object is an equal `PathGeometry` record instance. |
| `GetHashCode()` | Returns a hash code based on the record state. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Exceptions

| Constructor | Exception | Condition |
| --- | --- | --- |
| `PathGeometry(IEnumerable<DrawPoint> points)` | `ArgumentNullException` | `points` is `null`. |
| `PathGeometry(IEnumerable<DrawPoint> points)` | `ArgumentException` | `points` contains no elements. |
| `PathGeometry(IEnumerable<DrawPoint> points)` | `ArgumentOutOfRangeException` | The calculated `DrawRect` bounds fail drawing-coordinate validation. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Stroke` resolves to a visible solid color and `StrokeThickness > 0` | `Shape` emits line drawing commands between consecutive points. |
| `Stroke` is transparent, missing, or `StrokeThickness` is `0` | No path line commands are emitted. |
| The path contains exactly one point | Bounds are valid, but no line commands are emitted because there are no consecutive point pairs. |
| `RenderTransform` is set on the shape | Each rendered segment endpoint is transformed before drawing. |

## Applies To

Cerneala retained UI media geometry APIs in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.Geometry`
- `Cerneala.UI.Controls.Shapes.Path`
- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawRect`
