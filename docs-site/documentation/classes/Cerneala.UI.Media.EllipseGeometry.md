# EllipseGeometry Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/EllipseGeometry.cs`

Represents an immutable ellipse geometry whose bounds are defined by a `DrawRect`.

```csharp
public sealed record EllipseGeometry(DrawRect Rect) : Geometry
```

Inheritance:
`object` -> `Geometry` -> `EllipseGeometry`

Implements:
`IEquatable<EllipseGeometry>`

## Examples

Create an ellipse geometry with explicit drawing bounds:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

DrawRect rect = new(10, 20, 120, 80);
EllipseGeometry geometry = new(rect);

DrawRect bounds = geometry.Bounds;
```

Use an `EllipseGeometry` as the custom geometry for a shape:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Ellipse ellipse = new()
{
    Geometry = new EllipseGeometry(new DrawRect(0, 0, 64, 64)),
    Fill = new SolidColorBrush(Color.White),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

## Remarks

`EllipseGeometry` is a small immutable geometry value. The primary constructor stores the supplied `DrawRect` in the `Rect` property, and the overridden `Bounds` property returns the same rectangle.

The rectangle describes the ellipse bounding box in drawing coordinates. Rendering code in `Shape` handles `EllipseGeometry` by transforming its bounds, then emitting fill and stroke ellipse drawing commands when the transformed width and height are both greater than zero.

Because `EllipseGeometry` is a sealed record, instances support value equality based on their record state. Two instances with equal `Rect` values compare as equal.

## Constructors

| Name | Description |
| --- | --- |
| `EllipseGeometry(DrawRect Rect)` | Initializes an ellipse geometry with the specified bounding rectangle. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Rect` | `DrawRect` | Gets the rectangle that defines the ellipse bounding box. |
| `Bounds` | `DrawRect` | Gets the geometry bounds. For `EllipseGeometry`, this returns `Rect`. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(EllipseGeometry?)` | Determines whether another `EllipseGeometry` has equal record state. |
| `Equals(object?)` | Determines whether an object is an equal `EllipseGeometry` record instance. |
| `GetHashCode()` | Returns a hash code based on the record state. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Fill` resolves to a visible solid color and transformed bounds are positive | `Shape` emits a fill ellipse command. |
| `Stroke` resolves to a visible solid color, `StrokeThickness > 0`, and transformed bounds are positive | `Shape` emits a stroke ellipse command. |
| Transformed width or height is `0` | Fill and stroke ellipse drawing are skipped by `Shape`. |

## Applies To

Cerneala retained UI media geometry APIs in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.Geometry`
- `Cerneala.UI.Controls.Shapes.Ellipse`
- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.DrawRect`
