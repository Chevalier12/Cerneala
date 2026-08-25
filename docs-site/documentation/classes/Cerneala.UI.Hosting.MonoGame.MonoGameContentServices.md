# MonoGameContentServices Class

## Definition
Namespace: `Cerneala.UI.Hosting.MonoGame`

Assembly/Project: `Cerneala.Backends.MonoGame`

Source: `UI/Hosting/MonoGame/MonoGameContentServices.cs`

Provides the compatibility name for backend-agnostic drawing content services used by MonoGame hosts.

```csharp
public sealed class MonoGameContentServices : DrawingContentServices
```

Inheritance: `Object` -> `DrawingContentServices` -> `MonoGameContentServices`

## Examples

```csharp
using Cerneala.UI.Hosting.MonoGame;

using MonoGameContentServices services = new();
IDrawFont font = services.LoadFont("Arial", 16);
SkiaTextRasterizer rasterizer = services.TextRasterizer;
```

## Remarks

`MonoGameContentServices` is a compatibility subclass of `DrawingContentServices`. New backend-independent code can construct and pass `DrawingContentServices` directly.

`ImageResourceCache` is always created from the optional image loader. Disposing the service disposes the image resource cache and is idempotent.

## Constructors

| Name | Description |
| --- | --- |
| `MonoGameContentServices(IFontSource?, SkiaTextRasterizer?, IImageLoader?)` | Initializes content services with optional font, rasterizer, and image loader dependencies. |

## Properties

| Name | Description |
| --- | --- |
| `FontSource` | Gets the font source used by `LoadFont`. Inherited from `DrawingContentServices`. |
| `TextRasterizer` | Gets the Skia text rasterizer. Inherited from `DrawingContentServices`. |
| `ImageLoader` | Gets the optional image loader. Inherited from `DrawingContentServices`. |
| `ImageResourceCache` | Gets the image resource cache associated with the image loader. Inherited from `DrawingContentServices`. |

## Methods

| Name | Description |
| --- | --- |
| `LoadFont(string, float)` | Loads a draw font from the configured font source. Inherited from `DrawingContentServices`. |
| `Dispose()` | Disposes the image resource cache once. Inherited from `DrawingContentServices`. |

## Applies to

Cerneala MonoGame UI hosting.

## See also

- `Cerneala.Drawing.Text.SystemFontSource`
- `Cerneala.UI.Hosting.DrawingContentServices`
- `Cerneala.Drawing.Text.SkiaTextRasterizer`
- `Cerneala.UI.Resources.ImageResourceCache`
