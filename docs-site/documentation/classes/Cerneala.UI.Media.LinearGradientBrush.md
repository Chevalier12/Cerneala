# LinearGradientBrush Class

## Definition
Namespace: `Cerneala.UI.Media`
Assembly/Project: `Cerneala`
Source: `UI/Media/LinearGradientBrush.cs`

Interpolates ordered colors between two Cerneala points.

```csharp
public sealed record LinearGradientBrush : Brush
```

## Examples
```csharp
shape.Fill = new LinearGradientBrush(
    new DrawPoint(0, 0), new DrawPoint(100, 0),
    [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
```

## Remarks
Stops are copied, sorted by offset, and required to contain at least one item. Points and opacity must be finite. Equality compares points, opacity, and stop values structurally.

## Constructors
| Name | Description |
| --- | --- |
| `LinearGradientBrush(DrawPoint, DrawPoint, IEnumerable<GradientStop>, float)` | Creates a linear gradient; opacity defaults to `1`. |

## Properties
| Name | Description |
| --- | --- |
| `StartPoint` | Gradient start. |
| `EndPoint` | Gradient end. |
| `Stops` | Read-only ordered stops. |
| `Kind` | Always `DrawBrushKind.LinearGradient`. |
| `Opacity` | Inherited brush opacity. |

## Applies to
Shape rendering and backend brush descriptors.
