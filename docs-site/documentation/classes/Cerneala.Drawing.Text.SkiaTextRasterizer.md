# SkiaTextRasterizer Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `Drawing/Text/SkiaTextRasterizer.cs`

Rasterizes a `DrawTextRun` into an RGBA pixel buffer by shaping the text with `SkiaTextShaper` and drawing the resulting glyphs with SkiaSharp.

```csharp
public sealed class SkiaTextRasterizer
```

Inheritance:
`object` -> `SkiaTextRasterizer`

## Examples
Rasterize a text run loaded from the system font source:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

SystemFontSource fonts = new();
IDrawFont font = fonts.LoadFont("Arial", 16);

DrawTextRun textRun = new(font, "Cerneala", 16);
SkiaTextRasterizer rasterizer = new();

RasterizedText rasterized = rasterizer.Rasterize(textRun, DrawColor.Black);

int width = rasterized.Width;
int height = rasterized.Height;
byte[] pixels = rasterized.RgbaPixels;
```

## Remarks
`SkiaTextRasterizer` is the Skia-backed text rasterization step used by the drawing text pipeline. It requires text runs whose `DrawTextRun.Font` is a `SkiaFont`; other `IDrawFont` implementations are rejected.

`Rasterize` first delegates shaping to the configured `SkiaTextShaper`. The shaped glyph identifiers and positions are then packed into an `SKTextBlob`, drawn into an `SKBitmap` with antialiasing enabled, and returned as a `RasterizedText` instance.

The generated bitmap uses `SKColorType.Rgba8888` and premultiplied alpha. The method creates at least a `1` by `1` result, including for text that produces no glyphs. For glyph content, the rasterizer computes bitmap dimensions from the text blob bounds, trims fully transparent columns from the left edge when possible, and records the adjusted drawing origin in `RasterizedText.OriginOffset`.

The `DrawColor` argument is converted directly to an `SKColor` using its red, green, blue, and alpha components.

## Constructors
| Name | Description |
| --- | --- |
| `SkiaTextRasterizer()` | Initializes a rasterizer with a new `SkiaTextShaper`. |
| `SkiaTextRasterizer(SkiaTextShaper textShaper)` | Initializes a rasterizer that uses the supplied text shaper. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Rasterize(DrawTextRun textRun, DrawColor color)` | `RasterizedText` | Shapes and rasterizes `textRun` into an RGBA pixel buffer using the supplied text color. |

## Exceptions
| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `textShaper` is `null` in the constructor, or `textRun` is `null` in `Rasterize`. |
| `InvalidOperationException` | `textRun.Font` is not a `SkiaFont`, the shaped text blob cannot be built, or the underlying shaper cannot shape the supplied font data. |

## Applies to
Cerneala's Skia-backed drawing text pipeline, including `MonoGameDrawingBackend` and `MonoGameContentServices` integrations that use `SkiaTextRasterizer` for text texture generation.

## See also
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.RasterizedText`
- `Cerneala.Drawing.Text.SkiaFont`
- `Cerneala.Drawing.Text.SkiaTextShaper`
- `Cerneala.Drawing.Text.SystemFontSource`
