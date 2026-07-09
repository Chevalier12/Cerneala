# MonoGameUiHostOptions Class

## Definition
Namespace: `Cerneala.UI.Hosting.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`

Provides construction options for `MonoGameUiHost`.

```csharp
public sealed class MonoGameUiHostOptions
```

## Examples

```csharp
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;

MonoGameUiHostOptions options = new()
{
    SpriteBatch = spriteBatch,
    WhitePixel = whitePixel,
    Root = root,
    Viewport = new UiViewport(800, 600)
};
```

## Remarks

`SpriteBatch` and `WhitePixel` are required because the MonoGame host needs them to construct the drawing backend. Optional services let callers inject input, content, image loading, clock, text rasterization, and platform integrations.

When `MonoGameUiHost` is constructed, missing optional services are filled with defaults where supported. For example, a missing input source creates a `MonoGameInputSource`, and a missing content service creates `MonoGameContentServices`.

## Properties

| Name | Description |
| --- | --- |
| `SpriteBatch` | Gets the required sprite batch used for drawing. |
| `WhitePixel` | Gets the required one-pixel texture used by primitive drawing. |
| `Root` | Gets the optional initial UI root. |
| `Viewport` | Gets the initial viewport. Defaults to `new UiViewport(0, 0)`. |
| `InputSource` | Gets the optional MonoGame input source. |
| `ContentServices` | Gets optional content services. |
| `ImageLoader` | Gets an optional image loader used when content services are not supplied. |
| `Clock` | Gets the optional UI clock. |
| `TextRasterizer` | Gets an optional text rasterizer used when content services are not supplied. |
| `PlatformServices` | Gets optional platform services. |

## Applies to

Cerneala MonoGame UI hosting.

## See also

- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHost`
- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
- `Cerneala.UI.Hosting.UiViewport`
