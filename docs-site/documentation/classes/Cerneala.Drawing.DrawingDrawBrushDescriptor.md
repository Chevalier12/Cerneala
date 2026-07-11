# DrawingDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Backend descriptor containing immutable draw commands for a `DrawingBrush`.

```csharp
public sealed record DrawingDrawBrushDescriptor(
    IReadOnlyList<DrawCommand> Commands,
    DrawRect ContentBounds,
    DrawBrushStretch Stretch, DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY, DrawRect? Viewport,
    DrawRect? Viewbox, DrawTileMode TileMode, float BrushOpacity)
    : TileDrawBrushDescriptor(Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, BrushOpacity)
```

## Examples
```csharp
var descriptor = (DrawingDrawBrushDescriptor)drawingBrush.CreateDescriptor();
int commandCount = descriptor.Commands.Count;
```

## Remarks
The MonoGame backend rasterizes the command list into a device-owned render target before applying tile fitting, clipping, and opacity.

## Properties
| Name | Description |
| --- | --- |
| `Commands` | Immutable snapshot of commands to rasterize. |
| `ContentBounds` | Coordinate bounds of the command snapshot. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode` | Inherited tile settings. |
| `BrushOpacity` | Source opacity. |

## Applies to
MonoGame backend implementation code.
