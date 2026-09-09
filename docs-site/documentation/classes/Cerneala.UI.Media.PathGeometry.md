# PathGeometry Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/PathGeometry.cs`

Represents immutable typed path geometry, constructed from connected points, a `DrawPath`, or SVG path data.

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

`FromPath(DrawPath)` retains the supplied immutable path without flattening or rebuilding it. `Parse(string)` delegates to `DrawPathParser.ParseSvg`, preserving lines, quadratic and cubic curves, elliptical arcs, multiple contours, and explicit closure. The supported SVG commands are absolute and relative `M`, `L`, `H`, `V`, `C`, `S`, `Q`, `T`, `A`, and `Z`.

`PathGeometry` stores the supplied `IEnumerable<DrawPoint>` as a read-only point list. The constructor copies the sequence into an array before exposing it through `Points`, so later changes to the original collection do not change the geometry.

At least one point is required. Passing `null` throws `ArgumentNullException`; passing an empty sequence throws `ArgumentException`.

For the point-sequence constructor, `Bounds` is calculated from the minimum and maximum X and Y coordinates. A geometry with one point has a zero-width, zero-height bounds rectangle at that point and `Path` is `null`. For `FromPath` and `Parse`, `Bounds` is the typed path's conservative bound, including curve controls and arc extrema.

`Path` is the authoritative rich geometry. For rich paths, `Points` is a read-only list of contour start points and segment endpoints, excluding explicit close segments. It is not a flattened curve or a lossless representation of contour boundaries and controls. The legacy constructor preserves its supplied point sequence exactly.

`Shape` uses the same native `Path` for fill and stroke, without flattening curves into point chains. Coordinates are local to the control's arranged position; retained affine scopes apply its transform and ancestor transforms. `FillRule` on the shape selects non-zero or even-odd filling. A single-point geometry emits no drawing commands.

Because `PathGeometry` is a sealed record, instances use record equality. The `Points` property is a read-only wrapper around the copied array, so equality compares that wrapper reference rather than performing point-by-point sequence equality.

## Constructors

| Name | Description |
| --- | --- |
| `PathGeometry(IEnumerable<DrawPoint> points)` | Initializes a path geometry from one or more points and calculates its bounds. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<DrawPoint>` | Gets the original point snapshot or the rich path's ordered start points and endpoints. |
| `Path` | `DrawPath?` | Gets the reusable immutable typed path; `null` only for a legacy single-point geometry. |
| `Bounds` | `DrawRect` | Gets the point bounds or the rich path's conservative bounds. |

## Methods

| Name | Description |
| --- | --- |
| `FromPath(DrawPath)` | Creates a geometry retaining the supplied immutable typed path. Rejects `null`. |
| `Parse(string)` | Creates a geometry using the shared SVG path-data parser. |
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
| `FromPath(DrawPath)` | `ArgumentNullException` | `path` is `null`. |
| `Parse(string)` | `ArgumentException` | Data is null, empty, or whitespace. |
| `Parse(string)` | `FormatException` | SVG syntax is malformed or unsupported. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Fill` resolves to a visible brush, `Path` exists, and both bounds dimensions are positive | `Shape` emits a native typed-path fill command. |
| Bounds have zero width or height | Filling is skipped; stroke rendering remains available. |
| `Stroke` resolves to a visible brush, `StrokeThickness > 0`, and `Path` exists | `Shape` emits one native typed-path stroke command. |
| `Stroke` is transparent, missing, or `StrokeThickness` is `0` | No stroke command is emitted; filling is independent. |
| The path contains exactly one point | Bounds are valid, but no line commands are emitted because there are no consecutive point pairs. |
| `RenderTransform` is set on the shape | Retained affine drawing state transforms the complete path without changing its identity. |

## Applies To

Cerneala retained UI media geometry APIs in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.Geometry`
- `Cerneala.UI.Controls.Shapes.Path`
- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawRect`
