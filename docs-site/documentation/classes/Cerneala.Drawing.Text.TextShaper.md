# TextShaper Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `Drawing/Text/TextShaper.cs`

Provides a non-throwing facade for shaping and measuring Skia-backed drawing text runs.

```csharp
public sealed class TextShaper
```

Inheritance:
`object` -> `TextShaper`

## Examples
Shape text with the shared default shaper:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

SystemFontSource fonts = new();
IDrawFont font = fonts.LoadFont("Arial", 16);
DrawTextRun textRun = new(font, "Cerneala", 16);

if (TextShaper.Default.TryShape(textRun, out TextShapeResult shape))
{
    int glyphCount = shape.GlyphCount;
    float advanceWidth = shape.AdvanceWidth;
}
```

Measure caret-related vertical text metrics:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

SystemFontSource fonts = new();
DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Ag", 16);

if (TextShaper.Default.TryMeasureCaretVerticalMetrics(textRun, out TextCaretVerticalMetrics metrics))
{
    float caretOffsetY = metrics.OffsetY;
    float caretHeight = metrics.Height;
}
```

## Remarks
`TextShaper` wraps the Skia text shaping pipeline behind `Try...` methods. It accepts `DrawTextRun` instances whose `Font` is a `SkiaFont`; unsupported font implementations return `false` instead of throwing. This makes it useful for higher-level text layout code that can fall back to approximate metrics when the drawing backend cannot shape the run.

`TryShape` delegates to `SkiaTextShaper.Shape` and returns the shaped glyph IDs, positions, advance width, and origin offset in a `TextShapeResult`.

`TryMeasureLineHeight` and `TryMeasureCaretVerticalMetrics` both rasterize the sample text `"Ag"` with the run's font and size. The line-height method returns the rasterized height. The caret metrics method currently reports a top offset of `0` and the same rasterized height.

All public methods throw `ArgumentNullException` when `textRun` is `null`. For non-Skia fonts, the methods return `false` and assign `default` or `0` to the `out` value.

## Constructors
| Name | Description |
| --- | --- |
| `TextShaper()` | Initializes a new text shaper with its own Skia text shaper instance. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Default` | `TextShaper` | Gets the shared default text shaper instance. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `TryShape(DrawTextRun textRun, out TextShapeResult result)` | `bool` | Attempts to shape a Skia-backed text run and returns `true` when shaping succeeds. |
| `TryMeasureLineHeight(DrawTextRun textRun, out float lineHeight)` | `bool` | Attempts to measure line height by rasterizing the `"Ag"` sample text with the run's font and size. |
| `TryMeasureCaretVerticalMetrics(DrawTextRun textRun, out TextCaretVerticalMetrics metrics)` | `bool` | Attempts to measure caret vertical metrics from a rasterized `"Ag"` sample. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `TryShape(DrawTextRun, out TextShapeResult)` | `ArgumentNullException` | `textRun` is `null`. |
| `TryMeasureLineHeight(DrawTextRun, out float)` | `ArgumentNullException` | `textRun` is `null`. |
| `TryMeasureCaretVerticalMetrics(DrawTextRun, out TextCaretVerticalMetrics)` | `ArgumentNullException` | `textRun` is `null`. |

## Applies to
Cerneala drawing text shaping and UI text layout paths that use Skia-backed fonts.

## See also
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.SkiaTextShaper`
- `Cerneala.Drawing.Text.SkiaTextRasterizer`
- `Cerneala.Drawing.Text.TextShapeResult`
- `Cerneala.Drawing.Text.TextCaretVerticalMetrics`
