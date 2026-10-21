# TextRenderer Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextRenderer.cs`

Measures and draws text lines to a drawing context.

```csharp
public class TextRenderer
```

Inheritance:
`object` -> `TextRenderer`

## Examples

Render text and receive the measurement result:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

TextAspect aspect = new("Default", 16, foreground: new SolidColorBrush(Color.Black));

TextRenderer renderer = TextRenderer.Default;

TextMeasureResult result = renderer.Render(
    drawingContext,
    "A long label that should collapse",
    aspect,
    new LayoutSize(200, 40),
    position: new DrawPoint(0, 0));
```

## Remarks

`TextRenderer` uses a `TextMeasurer` to measure the supplied text before drawing. Empty text returns the measurement without drawing.

For non-empty text with a non-null `TextAspect.Foreground`, `Render` resolves the font, measures line height with `TextLineMetrics`, and draws each measured line with the complete brush. Ellipsis modes draw the collapsed line text returned by `TextMeasurer`, never the hidden suffix or hidden vertical lines. Solid, gradient, image, drawing, and visual brushes are preserved in the recorded command. A brush change does not alter the text layout key. The method throws `ArgumentNullException` when `drawingContext` or `text` is `null`.

`RenderParagraph` draws a paragraph that `TextMeasurer.MeasureParagraph` lays out for the same arguments (the renderer calls the measurer, so a cached layout is reused). For each layout run it records one `DrawText` command at the run position plus `position`, with the `Foreground` of the `TextSpan` that the run came from. When the span has `TextDecorations`, the text command is followed by one `FillRectangle` command for each flag, in the same brush, in this order: underline, strikethrough, overline, baseline. A span whose `Aspect.Foreground` is `null` draws neither text nor decorations. The decoration rectangles are as wide as the run (a justified space is included in the width of its run, so a decoration stays continuous). The rectangles come from `TextShaper.TryMeasureDecorations`, or from `TextDecorationMetrics.CreateFallback` when the font is not a Skia font:

| Flag | Rectangle (baseline `y` of the run) |
| --- | --- |
| `Underline` | Top at `y + UnderlineOffset`, height `UnderlineThickness`. |
| `Strikethrough` | Bottom at `y + StrikethroughOffset`, height `StrikethroughThickness`. |
| `Overline` | Top at the top of the line box, height `UnderlineThickness`. |
| `Baseline` | Top at `y`, height `UnderlineThickness`. |

`RenderParagraph` throws `ArgumentNullException` when `drawingContext`, `spans`, or `options` is `null`.

## Constructors

| Signature | Description |
| --- | --- |
| `TextRenderer()` | Initializes a renderer with default font resolution and measurement services. |
| `TextRenderer(FontResolver fontResolver, TextMeasurer textMeasurer)` | Initializes a renderer with explicit services. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `TextRenderer` | Gets the default renderer instance. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(DrawingContext drawingContext, string text, TextAspect aspect, float availableWidth, DrawPoint position)` | `TextMeasureResult` | Measures text, draws each line when text and `aspect.Foreground` are present, and returns the measurement. |
| `Render(DrawingContext drawingContext, string text, TextAspect aspect, LayoutSize availableSize, DrawPoint position)` | `TextMeasureResult` | Measures and draws only the lines visible inside the supplied width and height. |
| `RenderParagraph(DrawingContext drawingContext, IReadOnlyList<TextSpan> spans, TextAspect paragraphAspect, TextParagraphOptions options, LayoutSize availableSize, DrawPoint position)` | `TextParagraphLayout` | Measures the spans as one paragraph and draws every run with the brush of its span, then the decorations of the span. Returns the layout. |

## Applies To

Cerneala UI text rendering.

## See Also

- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.FontResolver`
- `Cerneala.UI.Text.TextSpan`
- `Cerneala.UI.Text.TextParagraphLayout`
- `Cerneala.UI.Text.TextDecorations`
- `Cerneala.Drawing.DrawingContext`
