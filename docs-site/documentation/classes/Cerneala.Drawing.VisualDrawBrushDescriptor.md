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
var descriptor = (VisualDrawBrushDescriptor)visualBrush.CreateDescriptor();
```

## Remarks
`VisualIdentity` identifies the source visual for resource ownership and diagnostics. The capture path rejects recursive brush graphs before a render pass can recurse indefinitely.

## Properties
| Name | Description |
| --- | --- |
| `VisualIdentity` | Source visual identity. |
| `Commands` | Captured visual commands. |
| `ContentBounds` | Captured coordinate bounds. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode` | Inherited tile settings. |
| `BrushOpacity` | Source opacity. |

## Applies to
MonoGame backend implementation code.
