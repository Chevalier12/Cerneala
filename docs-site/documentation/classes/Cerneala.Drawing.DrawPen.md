# DrawPen Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawStrokeStyle.cs`

Combines a brush, positive stroke thickness, and immutable stroke style for native vector outlines.

```csharp
public sealed record DrawPen
```

## Examples

```csharp
DrawPen pen = new(
    accentBrush,
    thickness: 3,
    new DrawStrokeStyle(
        startCap: DrawLineCap.Round,
        endCap: DrawLineCap.Round,
        join: DrawLineJoin.Round,
        dashPattern: [8, 4]));

drawing.DrawPath(path, pen);
```

## Remarks

`DrawPen` is retained as part of a stroke command, so changing the brush, thickness, or style changes command equality and the stroke-mesh cache key. `Thickness` is measured in logical drawing units and is mapped to physical pixels by the active surface scale.

The constructor rejects a null brush and a thickness that is zero, negative, non-finite, or outside the supported pixel-size range. Omitting `style` uses `DrawStrokeStyle.Default`.

The MonoGame native-stroke backend supports solid, linear-gradient, and radial-gradient brushes.

## Constructors

| Name | Description |
| --- | --- |
| `DrawPen(IDrawBrush brush, float thickness, DrawStrokeStyle? style = null)` | Creates a pen and validates its brush and thickness. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Brush` | `IDrawBrush` | Gets the paint applied to the stroke mesh. |
| `Thickness` | `float` | Gets the positive stroke thickness in logical drawing units. |
| `Style` | `DrawStrokeStyle` | Gets the immutable cap, join, dash, miter, and alignment settings. |

## Applies To

Line, rectangle, ellipse, typed-path, and `RenderSurface2DFrame` stroke operations.

## See Also

- `DrawStrokeStyle`
- `DrawingContext.DrawPath`
- `DrawLineCap`
- `DrawLineJoin`
- `DrawStrokeAlignment`
