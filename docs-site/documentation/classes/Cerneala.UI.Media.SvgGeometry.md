# SvgGeometry Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/SvgGeometry.cs`

Represents immutable SVG path data together with the view box used to scale it.

```csharp
public sealed record SvgGeometry : Geometry
```

Inheritance:
`object` -> `Geometry` -> `SvgGeometry`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;
using PathShape = Cerneala.UI.Controls.Shapes.Path;

PathShape check = new()
{
    Geometry = new SvgGeometry("M2 8L6 12L14 3", new DrawRect(0, 0, 16, 16)),
    Fill = new SolidColorBrush(Color.Black)
};
```

## Remarks

The constructor parses `Data` once into `Path`. When assigned to `Shape.Geometry`, the cached typed path is stretched from `Bounds` into the shape's arranged bounds, avoiding per-frame SVG parsing. The shared parser supports move, line, cubic and quadratic Bezier, elliptical arc, and close commands. The MonoGame backend tessellates the typed contours and submits triangles directly to `GraphicsDevice`.

## Constructors

| Name | Description |
| --- | --- |
| `SvgGeometry(string data, DrawRect viewBox)` | Creates SVG geometry from path data and a positive-size view box. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Data` | `string` | Gets the SVG path-data string. |
| `Path` | `DrawPath` | Gets the immutable parsed path reused by rendering. |
| `Bounds` | `DrawRect` | Gets the source view box used during scaling. |

## Exceptions

| Constructor | Exception | Condition |
| --- | --- | --- |
| `SvgGeometry` | `ArgumentException` | `data` is null, empty, or whitespace. |
| `SvgGeometry` | `ArgumentOutOfRangeException` | `viewBox` has a non-positive width or height. |

## Applies To

Cerneala path shapes rendered by the MonoGame drawing backend.

## See Also

- `Cerneala.UI.Controls.Shapes.Path`
- `Cerneala.UI.Media.Geometry`
- `Cerneala.Drawing.DrawCommand.FillPath`
- `Cerneala.Drawing.DrawPath`
