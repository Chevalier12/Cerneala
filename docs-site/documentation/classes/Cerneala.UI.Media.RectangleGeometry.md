# RectangleGeometry Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/RectangleGeometry.cs`

Represents an immutable rectangle geometry whose bounds are defined by a `DrawRect`.

```csharp
public sealed record RectangleGeometry(DrawRect Rect) : Geometry
```

Inheritance:
`object` -> `Geometry` -> `RectangleGeometry`

Implements:
`IEquatable<RectangleGeometry>`

## Examples

Create a rectangle geometry with explicit drawing bounds:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

DrawRect rect = new(10, 20, 120, 80);
RectangleGeometry geometry = new(rect);

DrawRect bounds = geometry.Bounds;
```

Use a `RectangleGeometry` as the custom geometry for a shape:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

Rectangle rectangle = new()
{
    Geometry = new RectangleGeometry(new DrawRect(0, 0, 64, 32)),
    Fill = new SolidColorBrush(DrawColor.White),
    Stroke = new SolidColorBrush(DrawColor.Black),
    StrokeThickness = 2
};
```

## Remarks

`RectangleGeometry` is a small immutable geometry value. The primary constructor stores the supplied `DrawRect` in the `Rect` property, and the overridden `Bounds` property returns the same rectangle.

The rectangle describes the geometry bounds in drawing coordinates. Rendering code in `Shape` handles `RectangleGeometry` by transforming its bounds, then emitting fill and stroke rectangle drawing commands when the transformed width and height are both greater than zero.

Because `RectangleGeometry` is a sealed record, instances support value equality based on their record state. Two instances with equal `Rect` values compare as equal.

`RectangleGeometry` does not add validation beyond the `DrawRect` value it receives. Coordinate and size validation happens when the `DrawRect` instance is constructed.

## Constructors

| Name | Description |
| --- | --- |
| `RectangleGeometry(DrawRect Rect)` | Initializes a rectangle geometry with the specified bounding rectangle. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Rect` | `DrawRect` | Gets the rectangle that defines this geometry. |
| `Bounds` | `DrawRect` | Gets the geometry bounds. For `RectangleGeometry`, this returns `Rect`. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(RectangleGeometry?)` | Determines whether another `RectangleGeometry` has equal record state. |
| `Equals(object?)` | Determines whether an object is an equal `RectangleGeometry` record instance. |
| `GetHashCode()` | Returns a hash code based on the record state. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Rendering Behavior

| Condition | Result |
| --- | --- |
| `Fill` resolves to a visible solid color and transformed bounds are positive | `Shape` emits a fill rectangle command. |
| `Stroke` resolves to a visible solid color, `StrokeThickness > 0`, and transformed bounds are positive | `Shape` emits a stroke rectangle command. |
| Transformed width or height is `0` | Fill and stroke rectangle drawing are skipped by `Shape`. |

## Applies To

Cerneala retained UI media geometry APIs in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.Geometry`
- `Cerneala.UI.Controls.Shapes.Rectangle`
- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.DrawRect`
