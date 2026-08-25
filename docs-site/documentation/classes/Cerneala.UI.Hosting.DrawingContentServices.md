# DrawingContentServices Class

## Definition
Namespace: `Cerneala.UI.Hosting`  
Assembly/Project: `Cerneala`  
Source: `UI/Drawing/DrawingContentServices.cs`

Owns the MonoGame-independent font, Skia text rasterization, and backend-provided image resource services used by a UI host.

```csharp
public class DrawingContentServices : IDisposable
```

Implements: `IDisposable`

## Examples

```csharp
using DrawingContentServices services = new(imageLoader: imageLoader);
IDrawFont font = services.LoadFont("Arial", 16);
ImageResourceCache cache = services.ImageResourceCache;
```

## Remarks

The service creates a `SystemFontSource` and `SkiaTextRasterizer` when those dependencies are omitted. `ImageResourceCache` is always created from the optional `IImageLoader`.

The service owns its image resource cache. `Dispose` releases the cache once and is idempotent. The class contains no MonoGame or XNA types and can be supplied to any compatible UI host; text rasterization remains explicitly Skia-based.

## Constructors

| Name | Description |
| --- | --- |
| `DrawingContentServices(IFontSource?, SkiaTextRasterizer?, IImageLoader?)` | Initializes content services with optional font, rasterizer, and image loader dependencies. |

## Properties

| Name | Description |
| --- | --- |
| `FontSource` | Gets the font source used by `LoadFont`. |
| `TextRasterizer` | Gets the Skia text rasterizer. |
| `ImageLoader` | Gets the optional image loader. |
| `ImageResourceCache` | Gets the image resource cache associated with the image loader. |

## Methods

| Name | Description |
| --- | --- |
| `LoadFont(string, float)` | Loads a draw font from the configured font source. |
| `Dispose()` | Disposes the image resource cache once. |

## Applies to

Cerneala UI hosting that uses the core Skia text pipeline.

## See also

- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
- `Cerneala.Drawing.Text.SystemFontSource`
- `Cerneala.UI.Resources.ImageResourceCache`
