# RadialGradientDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Backend descriptor for an elliptical radial gradient.

```csharp
public sealed record RadialGradientDrawBrushDescriptor(
    DrawPoint Center, float RadiusX, float RadiusY,
    IReadOnlyList<DrawGradientStop> Stops, float BrushOpacity)
    : DrawBrushDescriptor(BrushOpacity)
```

## Examples
```csharp
var descriptor = new RadialGradientDrawBrushDescriptor(
    new DrawPoint(20, 20), 20, 12,
    [new DrawGradientStop(0, Color.White), new DrawGradientStop(1, Color.Black)],
    1);
```

## Remarks
The radii are independent, so the gradient can be elliptical. The MonoGame backend caches the generated device resource per graphics device.

## Properties
| Name | Description |
| --- | --- |
| `Center` | Center point. |
| `RadiusX` | Horizontal radius. |
| `RadiusY` | Vertical radius. |
| `Stops` | Ordered gradient stops. |
| `BrushOpacity` | Source opacity. |

## Applies to
Backend implementation code.
