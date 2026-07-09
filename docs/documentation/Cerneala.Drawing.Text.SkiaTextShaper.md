# SkiaTextShaper Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/Text/SkiaTextShaper.cs`

Shapes `DrawTextRun` text with Skia-backed fonts and HarfBuzz glyph positioning.

```csharp
public sealed class SkiaTextShaper
```

Inheritance:
`object` -> `SkiaTextShaper`

## Examples
Shape text loaded from the system font source:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

SystemFontSource fonts = new();
IDrawFont font = fonts.LoadFont("Arial", 16);
DrawTextRun textRun = new(font, "Cerneala", 16);

SkiaTextShaper shaper = new();
TextShapeResult shape = shaper.Shape(textRun);

int glyphCount = shape.GlyphCount;
float advanceWidth = shape.AdvanceWidth;
ushort[] glyphIds = shape.GlyphIds;
DrawPoint[] glyphPositions = shape.GlyphPositions;
```

## Remarks
`SkiaTextShaper` is the low-level shaping implementation used by the Skia text pipeline. It accepts a `DrawTextRun`, requires that the run's `Font` is a `SkiaFont`, and returns a `TextShapeResult` containing the original text, glyph count, glyph IDs, glyph positions, total advance width, and origin offset.

The shaper builds a HarfBuzz buffer from the run text as UTF-16, lets HarfBuzz infer segment properties, creates a HarfBuzz font from the `SkiaFont` typeface data, and shapes the buffer at the requested run size. HarfBuzz values are converted from 26.6 fixed-point units to drawing pixels by dividing by `64`.

Glyph positions are accumulated in drawing coordinates. Horizontal advances increase `X`; HarfBuzz vertical offsets and advances are inverted into drawing `Y` coordinates. `AdvanceWidth` is the accumulated horizontal advance after all glyphs are processed.

`OriginOffset` is calculated from a positioned Skia text blob built from the shaped glyph IDs and positions. Empty text can produce zero glyphs; in that case the origin offset is `default`.

Use `TextShaper.TryShape` when callers need a non-throwing facade for unsupported font implementations. Use `SkiaTextShaper.Shape` directly when the caller already owns a Skia-backed text run and wants the concrete shaping result.

## Constructors
| Name | Description |
| --- | --- |
| `SkiaTextShaper()` | Initializes a new Skia text shaper. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Shape(DrawTextRun textRun)` | `TextShapeResult` | Shapes a non-null Skia-backed text run and returns glyph IDs, glyph positions, advance width, and origin offset. |

## Exceptions
| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `textRun` is `null`. |
| `InvalidOperationException` | `textRun.Font` is not a `SkiaFont`, the font data read from the Skia typeface is empty, or a Skia text blob cannot be built for origin-offset calculation. |

## Applies to
Skia-backed text shaping in the Cerneala drawing text pipeline.

## See also
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.TextShapeResult`
- `Cerneala.Drawing.Text.SkiaFont`
- `Cerneala.Drawing.Text.TextShaper`
- `Cerneala.Drawing.Text.SkiaTextRasterizer`
