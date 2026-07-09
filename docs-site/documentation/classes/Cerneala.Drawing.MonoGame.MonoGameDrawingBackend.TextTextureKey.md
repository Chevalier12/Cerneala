# MonoGameDrawingBackend.TextTextureKey Record

## Definition
Namespace: `Cerneala.Drawing.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`

Provides the private cache key used by `MonoGameDrawingBackend` for rasterized text textures.

```csharp
private readonly record struct TextTextureKey(
    string Text,
    IDrawFont Font,
    float FontSize,
    DrawColor Color)
```

Containing type:
`MonoGameDrawingBackend`

## Examples

```csharp
DrawTextRun mappedTextRun = mapper.MapTextRun(command.TextRun);
TextTextureKey key = TextTextureKey.From(mappedTextRun, command.Color);
```

## Remarks

`TextTextureKey` is an implementation detail of `MonoGameDrawingBackend`. It groups the text content, draw font, mapped font size, and draw color so rendered text textures can be reused from the backend's text texture cache.

`From` builds the key from a `DrawTextRun` and a `DrawColor`. The backend uses that key before rasterizing text; when the key is already present, the cached `Texture2D` is reused.

## Constructors

| Name | Description |
| --- | --- |
| `TextTextureKey(string, IDrawFont, float, DrawColor)` | Initializes a text texture cache key. |

## Properties

| Name | Description |
| --- | --- |
| `Text` | Gets the text content used for rasterization. |
| `Font` | Gets the draw font used for rasterization. |
| `FontSize` | Gets the mapped font size. |
| `Color` | Gets the draw color used for rasterization. |

## Methods

| Name | Description |
| --- | --- |
| `From(DrawTextRun, DrawColor)` | Creates a cache key from a text run and draw color. |

## Applies to

Cerneala MonoGame drawing backend internals.

## See also

- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.RasterizedText`
