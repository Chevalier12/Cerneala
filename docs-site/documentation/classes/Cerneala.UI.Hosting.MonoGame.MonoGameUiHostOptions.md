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
using Cerneala.Drawing.Prism;

MonoGameUiHostOptions options = new()
{
    SpriteBatch = spriteBatch,
    WhitePixel = whitePixel,
    Root = root,
    Viewport = new UiViewport(800, 600),
    PrismRendererOptions = new PrismRendererOptions
    {
        RetainedCacheSoftByteLimit = 128L * 1024 * 1024,
        RetainedCacheEntryLimit = 128
    }
};
```

## Remarks

`SpriteBatch` and `WhitePixel` are required because the MonoGame host needs them
to construct the drawing backend. Both resources must be alive and use the same
`GraphicsDevice`. The host and backend borrow the sprite batch, texture, and
device; the caller keeps ownership and disposes them after the host.

When `MonoGameUiHost` is constructed, missing optional services are filled with defaults where supported. For example, a missing input source creates a `MonoGameInputSource`, and a missing content service creates `MonoGameContentServices`.

When `ContentServices` is supplied, the host takes ownership of that service
object and disposes it with the host. `ImageLoader` and `TextRasterizer` are used
only when `ContentServices` is not supplied.

`BackdropFrameSource` is optional and remains application-owned. The application
also keeps ownership of the already-rendered scene and its GPU resources.
Cerneala acquires and disposes only a frame-scoped lease while drawing; it does
not dispose the source or retain its scene after the frame. A MonoGame source
must be compatible with the host backend's live `GraphicsDevice`. The WindowsDX
source uses device compatibility rather than backend-instance identity, so the
same source works for both on-screen and temporary screenshot backends created
on that device.

`PrismRendererOptions` is optional. When it is `null`, the host creates default
Prism options. When supplied, its total-surface hard limit, retained soft limit,
entry limit, and development-diagnostic switch are passed to the owned drawing
backend and validated during host construction.

## Properties

| Name | Description |
| --- | --- |
| `SpriteBatch` | Gets the required caller-owned sprite batch borrowed by the drawing backend. It must be alive and inactive when the host draws. |
| `WhitePixel` | Gets the required caller-owned one-pixel texture. It must be alive and belong to the sprite batch's graphics device. |
| `Root` | Gets the optional initial UI root. |
| `Viewport` | Gets the initial viewport. Defaults to `new UiViewport(0, 0)`. |
| `InputSource` | Gets the optional MonoGame input source. |
| `ContentServices` | Gets optional content services whose lifetime is transferred to the host. |
| `ImageLoader` | Gets an optional image loader used when content services are not supplied. |
| `Clock` | Gets the optional UI clock. |
| `TextRasterizer` | Gets an optional text rasterizer used when content services are not supplied. |
| `PlatformServices` | Gets optional platform services. |
| `BackdropFrameSource` | Gets the optional application-owned source of already-rendered backdrop frames. |
| `PrismRendererOptions` | Gets optional Prism surface-budget and development-diagnostic options. `null` selects `PrismRendererOptions` defaults. |

## Validation

Constructing `MonoGameUiHost` validates the required graphics resources.

| Condition | Exception |
| --- | --- |
| `SpriteBatch` or `WhitePixel` is `null`. | `ArgumentNullException` |
| A required resource or its graphics device is disposed. | `ObjectDisposedException` |
| `WhitePixel` belongs to a different graphics device than `SpriteBatch`. | `ArgumentException` |
| `BackdropFrameSource` cannot supply leases consumable by the created live MonoGame drawing backend. | `InvalidOperationException` |
| A Prism byte or entry limit is negative, or the retained soft limit exceeds the hard limit. | `ArgumentOutOfRangeException` |

## Applies to

Cerneala MonoGame UI hosting.

## See also

- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHost`
- `Cerneala.UI.Hosting.MonoGame.MonoGameContentServices`
- `Cerneala.UI.Hosting.UiViewport`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
- `Cerneala.Drawing.Prism.PrismRendererOptions`
