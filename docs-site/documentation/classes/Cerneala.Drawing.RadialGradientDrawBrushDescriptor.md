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
The radii are independent, so the gradient can be elliptical. The SDL_GPU backend caches the generated texture per graphics device, keyed by the brush, the drawn bounds, and the texture pixel size, so one brush drawn at two sizes owns two textures.

## Properties
| Name | Description |
| --- | --- |
| `Center` | Center point. |
| `RadiusX` | Horizontal radius. |
| `RadiusY` | Vertical radius. |
| `Stops` | Ordered gradient stops. |
| `BrushOpacity` | Source opacity; exposed as inherited `Opacity` too. |

## Applies to
Backend implementation code.
