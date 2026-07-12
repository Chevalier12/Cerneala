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

When assigned to `Shape.Geometry`, the SVG coordinates in `Data` are stretched from `Bounds` into the shape's arranged bounds. The MonoGame backend parses move, line, cubic and quadratic Bezier, elliptical arc, and close commands, tessellates the resulting contours, and submits triangles directly to `GraphicsDevice`. It supports solid fill brushes for SVG geometry.

## Constructors

| Name | Description |
| --- | --- |
| `SvgGeometry(string data, DrawRect viewBox)` | Creates SVG geometry from path data and a positive-size view box. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Data` | `string` | Gets the SVG path-data string. |
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
