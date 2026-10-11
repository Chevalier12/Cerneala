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

The constructor parses `Data` once into `Path`. When assigned to `Shape.Geometry`, the cached typed path is stretched from `Bounds` into the shape's arranged bounds, avoiding per-frame SVG parsing. The shared parser supports move, line, horizontal and vertical line, cubic and quadratic Bezier (including the smooth forms), elliptical arc, and close commands, in absolute and relative form. An unknown command or malformed data throws `FormatException`. The SDL_GPU backend uses the shared path tessellator to submit triangles.

## Constructors

| Name | Description |
| --- | --- |
| `SvgGeometry(string data, DrawRect viewBox)` | Creates SVG geometry from path data and a view box. A view box whose `Width` or `Height` is zero or negative is rejected; `NaN` sizes are not. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Data` | `string` | Gets the SVG path-data string. |
| `Path` | `DrawPath` | Gets the immutable parsed path reused by rendering. |
| `Bounds` | `DrawRect` | Gets the source view box used during scaling. |

## Exceptions

| Constructor | Exception | Condition |
| --- | --- | --- |
| `SvgGeometry` | `ArgumentNullException` | `data` is `null`. |
| `SvgGeometry` | `ArgumentException` | `data` is empty or whitespace. |
| `SvgGeometry` | `FormatException` | `data` contains an unknown command or malformed path data. |
| `SvgGeometry` | `ArgumentOutOfRangeException` | `viewBox` has a non-positive width or height. |

## Applies To

Cerneala path shapes rendered by the SDL_GPU drawing backend.

## See Also

- `Cerneala.UI.Controls.Shapes.Path`
- `Cerneala.UI.Media.Geometry`
- `Cerneala.Drawing.DrawCommand.FillPath`
- `Cerneala.Drawing.DrawPath`
