# DrawRect Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/DrawRect.cs`

Represents an immutable drawing-space rectangle using floating-point pixel coordinates and dimensions.

```csharp
public readonly record struct DrawRect
```

Inheritance:
`Object` -> `ValueType` -> `DrawRect`

Implements:
`IEquatable<DrawRect>`

## Examples

Create a rectangle and use it with a drawing context:

```csharp
using Cerneala.Drawing;

DrawRect bounds = new(10, 20, 120, 40);

context.DrawingContext.FillRectangle(bounds, DrawColor.White);
context.DrawingContext.DrawRectangle(bounds, DrawColor.Black, thickness: 1);
```

Use the derived edges when calculating adjacent drawing bounds:

```csharp
DrawRect left = new(0, 0, 50, 20);
DrawRect right = new(left.Right, left.Y, 50, left.Height);
```

## Remarks

`DrawRect` stores a rectangle as `X`, `Y`, `Width`, and `Height`. `Right` returns `X + Width`, and `Bottom` returns `Y + Height`.

The constructor validates all values before assigning them. `X` and `Y` must be finite pixel coordinates in the drawing range. `Width` and `Height` must be finite, non-negative pixel sizes. The computed right and bottom edges must also remain valid pixel coordinates. Invalid values throw `ArgumentOutOfRangeException`.

Because `DrawRect` is a readonly record struct, its values are immutable after construction and support value equality. As with any struct, `default(DrawRect)` is possible and represents a zero-sized rectangle at `(0, 0)`.

`DrawRect` is used by drawing commands and drawing contexts for rectangles, ellipses, image destinations, clip regions, geometry bounds, and retained rendering transforms.

## Constructors

| Name | Description |
| --- | --- |
| `DrawRect(float x, float y, float width, float height)` | Initializes a rectangle after validating the origin, size, and computed right and bottom edges. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `float` | The left coordinate of the rectangle. |
| `Y` | `float` | The top coordinate of the rectangle. |
| `Width` | `float` | The non-negative width of the rectangle. |
| `Height` | `float` | The non-negative height of the rectangle. |
| `Right` | `float` | The computed right edge, equal to `X + Width`. |
| `Bottom` | `float` | The computed bottom edge, equal to `Y + Height`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DrawRect(float x, float y, float width, float height)` | `ArgumentOutOfRangeException` | `x`, `y`, `width`, `height`, `x + width`, or `y + height` is outside the accepted drawing pixel range, or `width`/`height` is negative. |

## Applies To

`Cerneala` drawing APIs that consume `Cerneala.Drawing.DrawRect`, including `DrawingContext`, `DrawCommand`, geometry bounds, image destinations, and clipping.

## See Also

- `Cerneala.Drawing.DrawingContext`
- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.DrawColor`
