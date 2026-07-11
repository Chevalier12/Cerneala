# RadialGradientBrush Class

## Definition
Namespace: `Cerneala.UI.Media`
Assembly/Project: `Cerneala`
Source: `UI/Media/RadialGradientBrush.cs`

Interpolates ordered colors from a center point over independent horizontal and vertical radii.

```csharp
public sealed record RadialGradientBrush : Brush
```

## Examples
```csharp
shape.Fill = new RadialGradientBrush(
    new DrawPoint(40, 30), 40, 30,
    [new GradientStop(0, Color.White), new GradientStop(1, Color.Black)]);
```

## Remarks
Both radii must be finite and positive. Stops are copied and ordered exactly like linear gradients. Structural equality includes center, radii, opacity, and stop values.

## Constructors
| Name | Description |
| --- | --- |
| `RadialGradientBrush(DrawPoint, float, float, IEnumerable<GradientStop>, float)` | Creates an elliptical radial gradient; opacity defaults to `1`. |

## Properties
| Name | Description |
| --- | --- |
| `Center` | Gradient center. |
| `RadiusX` | Horizontal radius. |
| `RadiusY` | Vertical radius. |
| `Stops` | Read-only ordered stops. |
| `Kind` | Always `DrawBrushKind.RadialGradient`. |
| `Opacity` | Inherited brush opacity. |

## Applies to
Shape rendering and backend brush descriptors.
