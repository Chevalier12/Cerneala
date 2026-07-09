# MonoGameContentServices Class

## Definition
Namespace: `Cerneala.UI.Hosting.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/MonoGame/MonoGameContentServices.cs`

Provides MonoGame host content services for fonts, text rasterization, image loading, and image resource caching.

```csharp
public sealed class MonoGameContentServices : IDisposable
```

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.UI.Hosting.MonoGame;

using MonoGameContentServices services = new();
IDrawFont font = services.LoadFont("Arial", 16);
SkiaTextRasterizer rasterizer = services.TextRasterizer;
```

## Remarks

`MonoGameContentServices` owns the content services used by the MonoGame host integration. When no font source is provided, it creates a `SystemFontSource`. When no text rasterizer is provided, it creates a `SkiaTextRasterizer`.

`ImageResourceCache` is always created from the optional image loader. Disposing the service disposes the image resource cache and is idempotent.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameContentServices(IFontSource?, SkiaTextRasterizer?, IImageLoader?)` | Initializes content services with optional font, rasterizer, and image loader dependencies. |

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

Cerneala MonoGame UI hosting.

## See also

- `Cerneala.Drawing.Text.SystemFontSource`
- `Cerneala.Drawing.Text.SkiaTextRasterizer`
- `Cerneala.UI.Resources.ImageResourceCache`
