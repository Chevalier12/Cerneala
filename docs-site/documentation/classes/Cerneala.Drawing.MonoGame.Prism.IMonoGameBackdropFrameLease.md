# IMonoGameBackdropFrameLease Interface

## Definition
Namespace: `Cerneala.Drawing.MonoGame.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/Prism/IMonoGameBackdropFrameLease.cs`

Exposes a borrowed MonoGame texture for a backend-neutral Prism backdrop lease.

```csharp
public interface IMonoGameBackdropFrameLease : IBackdropFrameLease
```

Inherits:
`Cerneala.Drawing.Prism.IBackdropFrameLease`

## Examples

```csharp
using Cerneala.Drawing.MonoGame.Prism;
using Microsoft.Xna.Framework.Graphics;

static Texture2D GetFrameTexture(IMonoGameBackdropFrameLease lease)
{
    Texture2D texture = lease.Texture;
    if (texture.Width != lease.Metadata.PixelWidth ||
        texture.Height != lease.Metadata.PixelHeight)
    {
        throw new InvalidOperationException("Backdrop metadata does not match the texture.");
    }

    return texture;
}
```

## Remarks

This interface is the MonoGame-specific bridge for
`IBackdropFrameLease`. Generic hosting and graph analysis continue to depend
only on the backend-neutral lease, while the MonoGame Prism executor uses
`Texture` as a GPU input.

The texture is borrowed from the application. Disposing the lease ends the
frame-scoped borrow; it must not dispose the texture or transfer texture
ownership to Cerneala. `Texture` and `Metadata` are valid only until the lease
is disposed or the providing frame ends, whichever happens first.

The texture must belong to the same `GraphicsDevice` as the consuming
`MonoGameDrawingBackend`; the source does not require the consumer to be the
same backend instance that created the texture. Its dimensions and surface
format must match `Metadata`. The WindowsDX adapter supports `SurfaceFormat.Color`,
`SurfaceFormat.Bgra32`, and `SurfaceFormat.HalfVector4`; incompatible metadata
is reported through Prism fallback diagnostics instead of triggering a CPU
readback.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Texture` | `Texture2D` | Gets the application-owned texture borrowed for the current frame. |
| `Metadata` | `BackdropFrameMetadata` | Gets the raster, color, coordinate, and content-version metadata inherited from `IBackdropFrameLease`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Ends the frame-scoped borrow without disposing `Texture`. |

## Applies to

Cerneala Prism composition through the MonoGame and WindowsDX backends.

## See also

- `Cerneala.Drawing.Prism.IBackdropFrameLease`
- `Cerneala.Drawing.Prism.IBackdropFrameSource`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHost`
