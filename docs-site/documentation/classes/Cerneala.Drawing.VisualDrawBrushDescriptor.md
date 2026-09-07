# VisualDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Backend descriptor for a captured `VisualBrush` subtree.

```csharp
public sealed record VisualDrawBrushDescriptor(
    object VisualIdentity,
    IReadOnlyList<DrawCommand> Commands,
    DrawRect ContentBounds,
    DrawBrushStretch Stretch, DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY, DrawRect? Viewport,
    DrawRect? Viewbox, DrawTileMode TileMode, float BrushOpacity)
    : TileDrawBrushDescriptor(Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, BrushOpacity)
```

## Examples
```csharp
var descriptor = (VisualDrawBrushDescriptor)((IDrawBrush)visualBrush).CreateDescriptor();
```

## Remarks
`VisualIdentity` identifies the source visual. Capture and SDL_GPU brush rendering reject recursive source graphs with `InvalidOperationException`. The renderer compares recorded command snapshots so an unchanged capture can reuse its raster while a changed source is repainted.

## Properties
| Name | Description |
| --- | --- |
| `VisualIdentity` | Source visual identity. |
| `Commands` | Captured visual commands. |
| `ContentBounds` | Captured coordinate bounds. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode` | Inherited tile settings. |
| `BrushOpacity` | Source opacity. |

## Applies to
Backend-neutral drawing descriptors, implemented by SDL_GPU.
