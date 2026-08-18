# TextShaper Class

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `Drawing/Text/TextShaper.cs`

Provides a facade for shaping Skia-backed drawing text runs and reading their line metrics.

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

Read the baseline and line height used by the Skia text pipeline:

```csharp
if (TextShaper.Default.TryMeasureBaseline(textRun, out float baseline) &&
    TextShaper.Default.TryMeasureLineHeight(textRun, out float lineHeight))
{
    Console.WriteLine($"Baseline: {baseline}, line height: {lineHeight}");
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
`TextShaper` wraps the Skia text shaping and line-metrics pipeline behind `Try...` methods. It accepts `DrawTextRun` instances whose `Font` is a `SkiaFont`; unsupported font implementations return `false` and set the corresponding `out` value to its default instead of throwing. This makes it useful for higher-level text layout code that can fall back when the drawing backend cannot shape the run.

`TryShape` delegates to `SkiaTextShaper.Shape` and returns the shaped glyph IDs, positions, and advance width in a `TextShapeResult`. Raster placement is calculated later by `SkiaTextRasterizer`.

`TryMeasureLineHeight`, `TryMeasureBaseline`, and `TryMeasureCaretVerticalMetrics` read the cached line metrics for the run's Skia typeface at the run's size. The metrics come from OpenType horizontal font extents when available and fall back to Skia font metrics otherwise. `TryMeasureBaseline` returns the baseline distance, `TryMeasureLineHeight` returns the line height, and caret metrics currently use an offset of `0` with that line height.

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
| `TryMeasureLineHeight(DrawTextRun textRun, out float lineHeight)` | `bool` | Attempts to read the line height for the run's Skia typeface and size. |
| `TryMeasureBaseline(DrawTextRun textRun, out float baseline)` | `bool` | Attempts to read the baseline distance for the run's Skia typeface and size. |
| `TryMeasureCaretVerticalMetrics(DrawTextRun textRun, out TextCaretVerticalMetrics metrics)` | `bool` | Attempts to read caret metrics using a zero offset and the run's line height. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `TryShape(DrawTextRun, out TextShapeResult)` | `ArgumentNullException` | `textRun` is `null`. |
| `TryMeasureLineHeight(DrawTextRun, out float)` | `ArgumentNullException` | `textRun` is `null`. |
| `TryMeasureBaseline(DrawTextRun, out float)` | `ArgumentNullException` | `textRun` is `null`. |
| `TryMeasureCaretVerticalMetrics(DrawTextRun, out TextCaretVerticalMetrics)` | `ArgumentNullException` | `textRun` is `null`. |

## Applies to
Cerneala drawing text shaping and UI text layout paths that use Skia-backed fonts.

## See also
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.SkiaTextShaper`
- `Cerneala.Drawing.Text.SkiaTextRasterizer`
- `Cerneala.Drawing.Text.TextShapeResult`
- `Cerneala.Drawing.Text.TextCaretVerticalMetrics`
