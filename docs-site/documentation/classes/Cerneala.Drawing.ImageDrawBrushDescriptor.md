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
using Cerneala.Drawing;
using Cerneala.UI.Media;

IDrawBrush imageBrush = new ImageBrush("assets/logo.png");
ImageDrawBrushDescriptor descriptor = (ImageDrawBrushDescriptor)imageBrush.CreateDescriptor();
// descriptor.Image is null; descriptor.SourceIdentity is "assets/logo.png".
```

## Remarks
`Image` carries the resolved image consumed by the drawing backend. SDL images retain CPU pixel data and are uploaded into device-owned textures when rendered.

## Properties
| Name | Description |
| --- | --- |
| `Image` | Resolved image, when available. |
| `SourceIdentity` | Unresolved source identity, when present. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode` | Inherited tile settings. |
| `BrushOpacity` | Source opacity; exposed as inherited `Opacity` too. |

## Applies to
Drawing backend implementation code.
