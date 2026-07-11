# LinearGradientDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Backend descriptor for a linear gradient.

```csharp
public sealed record LinearGradientDrawBrushDescriptor(
    DrawPoint StartPoint,
    DrawPoint EndPoint,
    IReadOnlyList<DrawGradientStop> Stops,
    float BrushOpacity) : DrawBrushDescriptor(BrushOpacity)
```

## Examples
```csharp
var descriptor = new LinearGradientDrawBrushDescriptor(
    new DrawPoint(0, 0), new DrawPoint(100, 0),
    [new DrawGradientStop(0, Color.White), new DrawGradientStop(1, Color.Black)],
    1);
```

## Remarks
The MonoGame backend turns this descriptor into a cached device texture and applies the normal clip, transform, opacity, and coordinate-scale pipeline.

## Properties
| Name | Description |
| --- | --- |
| `StartPoint` | Gradient origin in Cerneala coordinates. |
| `EndPoint` | Gradient destination in Cerneala coordinates. |
| `Stops` | Ordered gradient stops. |
| `BrushOpacity` | Source opacity. |

## Applies to
Backend implementation code.
