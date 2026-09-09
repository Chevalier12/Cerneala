# Line Class

## Definition

Namespace: `Cerneala.UI.Controls.Shapes`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Shapes/Line.cs`

Draws a straight segment between two local points.

```csharp
public sealed class Line : Shape
```

Inheritance: `Object` → `UiObject` → `UIElement` → `Control` → `Shape` → `Line`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Line line = new()
{
    StartPoint = new DrawPoint(0, 0),
    EndPoint = new DrawPoint(80, 40),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

Equivalent `.crn` markup:

```xml
<Line StartPoint="0,0" EndPoint="80,40" Stroke="Black" StrokeThickness="2" />
```

## Remarks

Points are local to the control's arranged position. They are not stretched to its arranged size. The inherited affine transform, transform origin, and ancestor transforms apply through retained rendering.

The segment uses the shared immutable native-path renderer. Its geometry is reused until an endpoint changes. A visible `Stroke` and positive `StrokeThickness` draw the segment. A straight segment has no fillable area. Both endpoints default to `(0, 0)`.

An explicit inherited `Geometry` takes precedence over the endpoints. Endpoint changes affect measure and render; natural measurement uses the endpoint bounds and inherited stroke padding.

## Constructors

| Name | Description |
| --- | --- |
| `Line()` | Initializes a line with coincident zero endpoints and inherited shape defaults. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `StartPointProperty` | `UiProperty<DrawPoint>` | Identifies `StartPoint`; affects measure and render. |
| `EndPointProperty` | `UiProperty<DrawPoint>` | Identifies `EndPoint`; affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `StartPoint` | `DrawPoint` | Gets or sets the local start point. |
| `EndPoint` | `DrawPoint` | Gets or sets the local end point. |

## Methods

| Name | Description |
| --- | --- |
| `ResolveGeometry(LayoutRect)` | Protected override resolving explicit geometry or the cached segment. |

## See Also

- [Shape](Cerneala.UI.Controls.Shapes.Shape.md)
- [Polyline](Cerneala.UI.Controls.Shapes.Polyline.md)
- [Path](Cerneala.UI.Controls.Shapes.Path.md)
