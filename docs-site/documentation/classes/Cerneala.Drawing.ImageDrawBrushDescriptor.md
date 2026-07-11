# ImageDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Backend descriptor for an image tile brush.

```csharp
public sealed record ImageDrawBrushDescriptor(
    IDrawImage? Image, string? SourceIdentity,
    DrawBrushStretch Stretch, DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY, DrawRect? Viewport,
    DrawRect? Viewbox, DrawTileMode TileMode, float BrushOpacity)
    : TileDrawBrushDescriptor(Stretch, AlignmentX, AlignmentY, Viewport, Viewbox, TileMode, BrushOpacity)
```

## Examples
```csharp
ImageDrawBrushDescriptor descriptor = (ImageDrawBrushDescriptor)imageBrush.CreateDescriptor();
```

## Remarks
`Image` must belong to the graphics device rendering the descriptor. A non-null `SourceIdentity` with no resolved image produces an explicit backend diagnostic instead of silently drawing nothing.

## Properties
| Name | Description |
| --- | --- |
| `Image` | Device-local image, when resolved. |
| `SourceIdentity` | Unresolved source identity, when present. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode` | Inherited tile settings. |
| `BrushOpacity` | Source opacity. |

## Applies to
MonoGame backend implementation code.
