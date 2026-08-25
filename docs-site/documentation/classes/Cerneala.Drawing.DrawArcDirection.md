# DrawArcDirection Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawPathFactory.cs`

Specifies the direction in which a convenience arc sweeps.

```csharp
public enum DrawArcDirection
```

## Examples

```csharp
drawing.DrawArc(
    center: new DrawPoint(64, 64),
    radiusX: 40,
    radiusY: 24,
    startAngle: 0,
    sweepAngle: MathF.PI * 1.5f,
    pen,
    DrawArcDirection.CounterClockwise);
```

## Remarks

Convenience-shape angles are measured in radians. Cerneala drawing coordinates have a downward-positive Y axis, so increasing angles sweep clockwise.

## Values

| Name | Description |
| --- | --- |
| `Clockwise` | Sweeps in the direction of increasing angles. |
| `CounterClockwise` | Sweeps in the direction of decreasing angles. |

## Applies To

Arc, pie, and chord factories and drawing helpers.

## See Also

- `DrawPathFactory`
- `DrawingContext.DrawArc`
