# MonoGameDrawingBackend.TextTextureKey Record

## Definition
Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala.Backends.MonoGame`

Source: `Drawing/MonoGame/MonoGameDrawingBackend.cs`

Provides the private cache key used by `MonoGameDrawingBackend` for rasterized text textures.

```csharp
private readonly record struct TextTextureKey(
    string Text,
    object FontIdentity,
    float FontSize,
    float CoordinateScale,
    DrawPoint PixelPhase)
```

Containing type:
`MonoGameDrawingBackend`

## Examples

```csharp
TextTextureKey key = TextTextureKey.From(textRun, coordinateScale, pixelPhase);
```

## Remarks

`TextTextureKey` is an implementation detail of `MonoGameDrawingBackend`. It groups the text content, font identity, size, coordinate scale, and canonical subpixel phase needed to rasterize an LCD coverage mask.

Foreground color is intentionally excluded. The cached textures contain color-independent coverage, and the backend applies solid text color while drawing. Changing only `Foreground` therefore reuses the existing rasterization and GPU texture.

## Constructors

| Name | Description |
| --- | --- |
| `TextTextureKey(string, object, float, float, DrawPoint)` | Initializes a text texture cache key. |

## Properties

| Name | Description |
| --- | --- |
| `Text` | Gets the text content used for rasterization. |
| `FontIdentity` | Gets the stable font identity used for rasterization. |
| `FontSize` | Gets the logical font size. |
| `CoordinateScale` | Gets the logical-to-physical coordinate scale. |
| `PixelPhase` | Gets the canonical subpixel phase. |

## Methods

| Name | Description |
| --- | --- |
| `From(DrawTextRun, float, DrawPoint)` | Creates a cache key from a text run, coordinate scale, and canonical pixel phase. |

## Applies to

Cerneala MonoGame drawing backend internals.

## See also

- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.RasterizedText`
