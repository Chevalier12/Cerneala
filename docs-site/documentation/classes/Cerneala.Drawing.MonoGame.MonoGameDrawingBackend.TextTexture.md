# MonoGameDrawingBackend.TextTexture Record

## Definition
Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/MonoGameDrawingBackend.cs`

Stores a cached MonoGame texture and the rasterized text origin offset used by `MonoGameDrawingBackend`.

```csharp
private readonly record struct TextTexture(Texture2D Texture, DrawPoint OriginOffset)
```

Containing type:
`MonoGameDrawingBackend`

## Examples

```csharp
RasterizedText text = _textRasterizer.Rasterize(mappedTextRun, command.Color);
Texture2D texture = new(_spriteBatch.GraphicsDevice, text.Width, text.Height);
texture.SetData(text.RgbaPixels);

TextTexture cachedText = new(texture, text.OriginOffset);
_textTextureCache.Add(key, cachedText);
```

## Remarks

`TextTexture` is an implementation detail of `MonoGameDrawingBackend`. The backend creates it after rasterizing a `DrawTextRun` into RGBA pixels and uploading those pixels into a MonoGame `Texture2D`.

The `Texture` value is the GPU resource drawn by `SpriteBatch.Draw`. The `OriginOffset` value is copied from `RasterizedText.OriginOffset` and passed through the backend's text texture positioning helper when drawing cached text.

Cached `TextTexture` values live in the backend's private text texture cache. `Dispose` iterates through the cached values, disposes each `Texture2D`, and clears the cache. Calling `Dispose` more than once is allowed by the backend.

## Constructors

| Name | Description |
| --- | --- |
| `TextTexture(Texture2D, DrawPoint)` | Initializes a cached text texture entry with the uploaded texture and rasterized origin offset. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Texture` | `Texture2D` | Gets the MonoGame texture containing the rasterized text pixels. |
| `OriginOffset` | `DrawPoint` | Gets the rasterized text origin offset associated with the texture. |

## Applies to

Cerneala MonoGame drawing backend internals.

## See also

- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend.TextTextureKey`
- `Cerneala.Drawing.Text.RasterizedText`
- `Cerneala.Drawing.DrawTextRun`
