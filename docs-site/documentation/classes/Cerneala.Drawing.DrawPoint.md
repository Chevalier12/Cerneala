# DrawPoint Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala` (`net8.0`)

Source: [`Drawing/DrawPoint.cs`](../../Drawing/DrawPoint.cs)

Represents an immutable two-dimensional drawing coordinate with finite `float` components.

```csharp
public readonly record struct DrawPoint
```

Inheritance:
`Object` -> `ValueType` -> `DrawPoint`

Implements:
`IEquatable<DrawPoint>`

## Examples

Create points and pass them to a drawing command:

```csharp
using Cerneala.Drawing;

DrawPoint start = new(12, 24);
DrawPoint end = new(start.X + 20, start.Y);

DrawCommand command = DrawCommand.DrawLine(start, end, Color.Black, thickness: 1);
```

Use component values to derive another point:

```csharp
using Cerneala.Drawing;

DrawPoint origin = new(8, 16);
DrawPoint shifted = new(origin.X + 4, origin.Y + 10);
```

## Remarks

`DrawPoint` stores `X` and `Y` coordinates for the drawing layer. The constructor accepts any finite `float` values, including negative coordinates. `NaN`, positive infinity, and negative infinity are rejected with `ArgumentOutOfRangeException`.

The type is a `readonly record struct`, so values are immutable after construction and use value-based equality. Drawing APIs such as `DrawCommand.DrawLine` and `DrawCommand.DrawText` may apply additional pixel-range validation when a point is used in a concrete command.

## Constructors

| Name | Description |
| --- | --- |
| `DrawPoint(float x, float y)` | Initializes a point from finite `x` and `y` coordinate values. Throws `ArgumentOutOfRangeException` when either argument is not finite. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `float` | Gets the horizontal coordinate. |
| `Y` | `float` | Gets the vertical coordinate. |

## Applies To

Cerneala drawing primitives in the `Cerneala.Drawing` namespace.

## See Also

- [`DrawSize`](../../Drawing/DrawSize.cs)
- [`DrawRect`](../../Drawing/DrawRect.cs)
- [`DrawCommand`](../../Drawing/DrawCommand.cs)
