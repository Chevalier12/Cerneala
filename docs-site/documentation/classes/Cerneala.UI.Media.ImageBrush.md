# ImageBrush Class

## Definition
Namespace: `Cerneala.UI.Media`  
Assembly/Project: `Cerneala`  
Source: `UI/Media/ImageBrush.cs`

Fills geometry with a device image, an image source, or a resolvable source identity.

```csharp
public sealed record ImageBrush : TileBrush
```

## Examples
```csharp
ImageBrush brush = new(image, tileMode: DrawTileMode.FlipX, opacity: 0.9f);
rectangle.Fill = brush;
```

## Remarks
The MonoGame backend verifies that a `MonoGameImage` belongs to the active `GraphicsDevice`. Source identities must be resolved before rendering; unresolved identities report a diagnostic. Equality includes source identity and image reference, so cache keys remain deterministic.

## Constructors
| Name | Description |
| --- | --- |
| `ImageBrush(IDrawImage?, DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Uses an already-resolved drawing image. |
| `ImageBrush(string, DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Stores a source identity for later resolution. |
| `ImageBrush(ImageSource, DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Uses an image source and resolves its current image. |

## Properties
| Name | Description |
| --- | --- |
| `Image` | Resolved drawing image, if available. |
| `Source` | `ImageSource` used to resolve an image, if supplied. |
| `SourceIdentity` | Unresolved source identity, if supplied. |
| `Kind` | Always `DrawBrushKind.Image`. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode`, `Opacity` | Inherited brush settings. |

## Applies to
MonoGame and other `IDrawingBackend` implementations that support images.
