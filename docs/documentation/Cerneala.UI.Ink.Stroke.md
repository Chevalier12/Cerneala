# Stroke Class

## Definition
Namespace: `Cerneala.UI.Ink`

Assembly/Project: `Cerneala`

Source: `UI/Ink/Stroke.cs`

Represents one ink stroke as an ordered list of drawing points.

```csharp
public sealed class Stroke
```

Inheritance:
`object` -> `Cerneala.UI.Ink.Stroke`

## Examples
Create a stroke with points and append another point:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Ink;

Stroke stroke = new(new[]
{
    new DrawPoint(1, 2),
    new DrawPoint(3, 4)
});

stroke.AddPoint(new DrawPoint(5, 6));

int pointCount = stroke.Points.Count;
DrawPoint lastPoint = stroke.Points[^1];
```

Use a stroke with a `StrokeCollection`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Ink;

StrokeCollection strokes = new();
Stroke stroke = new();

stroke.AddPoint(new DrawPoint(0, 0));
stroke.AddPoint(new DrawPoint(10, 10));

strokes.Add(stroke);
```

## Remarks
`Stroke` stores points in insertion order. The optional constructor argument seeds the stroke with the supplied `DrawPoint` values, and `AddPoint` appends additional values to the end of the same stroke.

The `Points` property exposes a read-only view of the stroke's internal point list. It prevents callers from mutating the list directly, but it reflects points added later through `AddPoint`.

`Stroke` does not raise change notifications when points are added. Callers that own rendering or layout invalidation, such as `InkCanvas`, are responsible for reacting after mutating a stroke.

## Constructors
| Name | Description |
| --- | --- |
| `Stroke(IEnumerable<DrawPoint>? points = null)` | Initializes a new stroke. When `points` is not `null`, copies the supplied points into the stroke in enumeration order. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<DrawPoint>` | Gets the ordered points recorded for the stroke. |

## Methods
| Name | Description |
| --- | --- |
| `AddPoint(DrawPoint point)` | Appends `point` to the stroke. |

## Applies to
Cerneala ink primitives in the `Cerneala` project.

## See also
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.UI.Controls.InkCanvas`
- `Cerneala.UI.Ink.StrokeCollection`
