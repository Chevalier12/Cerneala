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
    Fill = new SolidColorBrush(Color.White),
    Stroke = new SolidColorBrush(Color.Black),
    StrokeThickness = 2
};
```

## Remarks

`RectangleGeometry` is a small immutable geometry value. The primary constructor stores the supplied `DrawRect` in the `Rect` property, and the overridden `Bounds` property returns the same rectangle.

The rectangle describes the geometry bounds in drawing coordinates. Rendering code in `Shape` handles `RectangleGeometry` by emitting fill and stroke rectangle drawing commands for `Bounds` as stored, without transforming them, when its width and height are both greater than zero. A brush counts as visible when its `Opacity` is greater than zero and, for a solid color brush, its color alpha is greater than zero; gradient, image, and visual brushes count as visible.

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
| `Fill` is a visible brush and the bounds are positive | `Shape` emits a fill rectangle command. |
| `Stroke` is a visible brush, `StrokeThickness > 0`, and the bounds are positive | `Shape` emits a stroke rectangle command. |
| Width or height is `0` or less | Fill and stroke rectangle drawing are skipped by `Shape`. |

## Applies To

Cerneala retained UI media geometry APIs in the `Cerneala` project.

## See Also

- `Cerneala.UI.Media.Geometry`
- `Cerneala.UI.Controls.Shapes.Rectangle`
- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.DrawRect`
