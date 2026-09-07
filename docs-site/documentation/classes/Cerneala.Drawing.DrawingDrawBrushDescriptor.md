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
var descriptor = (DrawingDrawBrushDescriptor)((IDrawBrush)drawingBrush).CreateDescriptor();
int commandCount = descriptor.Commands.Count;
```

## Remarks
SDL_GPU rasterizes the command list with its tile fitting and clipping into a device-owned render target, then applies brush and command opacity when painting the destination. Unchanged command snapshots can reuse the completed raster.

## Properties
| Name | Description |
| --- | --- |
| `Commands` | Immutable snapshot of commands to rasterize. |
| `ContentBounds` | Coordinate bounds of the command snapshot. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode` | Inherited tile settings. |
| `BrushOpacity` | Source opacity. |

## Applies to
Backend-neutral drawing descriptors, implemented by SDL_GPU.
