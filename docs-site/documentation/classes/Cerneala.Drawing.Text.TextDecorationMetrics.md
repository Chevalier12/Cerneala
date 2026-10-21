# TextDecorationMetrics Struct

## Definition

Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `Drawing/Text/TextDecorationMetrics.cs`

Holds where the underline and the strikethrough of a font sit relative to the text baseline, and how thick their strokes are, at one font size.

```csharp
public readonly record struct TextDecorationMetrics(
    float UnderlineOffset,
    float UnderlineThickness,
    float StrikethroughOffset,
    float StrikethroughThickness)
```

Inheritance:
`ValueType` -> `TextDecorationMetrics`

## Examples

Read the metrics of a run and compute the rectangle of its underline:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

IDrawFont font = new SystemFontSource().LoadFont("Segoe UI", 16);
DrawTextRun run = new(font, "Hello", 16);
float baselineY = 100;

if (TextShaper.Default.TryMeasureDecorations(run, out TextDecorationMetrics metrics))
{
    // The offset is the top of the underline stroke, measured down from the baseline.
    DrawRect underline = new(0, baselineY + metrics.UnderlineOffset, 40, metrics.UnderlineThickness);

    // The offset is the bottom of the strikethrough stroke; a negative value is above the baseline.
    float strikeBottom = baselineY + metrics.StrikethroughOffset;
    DrawRect strike = new(0, strikeBottom - metrics.StrikethroughThickness, 40, metrics.StrikethroughThickness);
}
```

Use the fallback metrics for a font that is not a Skia font:

```csharp
using Cerneala.Drawing.Text;

TextDecorationMetrics fallback = TextDecorationMetrics.CreateFallback(28);
// fallback.UnderlineOffset         = 2.8
// fallback.UnderlineThickness      = 2
// fallback.StrikethroughOffset     = -8.4
// fallback.StrikethroughThickness  = 2
```

## Remarks

All offsets are measured from the text baseline, and a positive offset is below the baseline. The two offsets do not name the same edge of their stroke, because they follow the Skia definitions:

- `UnderlineOffset` is the distance from the baseline to the top edge of the underline stroke.
- `StrikethroughOffset` is the distance from the baseline to the bottom edge of the strikethrough stroke (Skia's `StrikeoutPosition`). It is normally negative, because the strike is above the baseline.

A thickness is the height of its stroke.

`TextShaper.TryMeasureDecorations` reads the four values from the Skia font metrics of the run's typeface at the run's size, and caches them for each typeface and size. When the font does not report a position, the fallback position is used. When the font reports a thickness that is missing or not greater than zero, the fallback thickness is used.

`CreateFallback(size)` computes the metrics of a font that reports none: the thickness of both strokes is `max(1, size / 14)`, the underline offset is `size * 0.1`, and the strikethrough offset is `-(size * 0.3)`. `TextRenderer.RenderParagraph` uses it when `TryMeasureDecorations` returns `false`.

## Constructors

| Name | Description |
| --- | --- |
| `TextDecorationMetrics(float UnderlineOffset, float UnderlineThickness, float StrikethroughOffset, float StrikethroughThickness)` | Creates the metrics from the four values. Performs no validation. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `UnderlineOffset` | `float` | Distance from the baseline down to the top edge of the underline stroke. |
| `UnderlineThickness` | `float` | Height of the underline stroke. |
| `StrikethroughOffset` | `float` | Distance from the baseline to the bottom edge of the strikethrough stroke. Negative when the stroke is above the baseline. |
| `StrikethroughThickness` | `float` | Height of the strikethrough stroke. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateFallback(float size)` | `TextDecorationMetrics` | Creates the metrics for a font size when the font reports none. Static. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.Drawing.Text.TextShaper`
- `Cerneala.UI.Text.TextDecorations`
- `Cerneala.UI.Text.TextRenderer`
