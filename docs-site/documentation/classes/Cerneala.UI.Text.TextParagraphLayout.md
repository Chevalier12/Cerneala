# TextParagraphLayout Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextParagraphLayout.cs`

Holds the laid out lines of a paragraph: its measured size, its line count, and the Drawing text layout that the lines come from.

```csharp
public sealed class TextParagraphLayout
```

Inheritance:
`object` -> `TextParagraphLayout`

## Examples

Measure a two-line paragraph and read its lines:

```csharp
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Text;
using Cerneala.Drawing;

TextAspect aspect = new("Segoe UI", 16, foreground: new SolidColorBrush(Color.Black));

TextParagraphLayout paragraph = TextMeasurer.Default.MeasureParagraph(
    [new TextSpan("one\ntwo", aspect)],
    aspect,
    new TextParagraphOptions(),
    new LayoutSize(float.PositiveInfinity, float.PositiveInfinity));

int lines = paragraph.LineCount;                 // 2
LayoutSize size = paragraph.Size;                // widest line, height of both lines
string first = paragraph.Layout.Lines[0].Text;   // "one"
```

## Remarks

`TextMeasurer.MeasureParagraph` and `TextRenderer.RenderParagraph` return this type. `TextBlock` uses `Size` as its desired size.

`Size.Width` is the width of the widest line as the line breaker measured it. It is not the width of the layout: with `TextAlignment.Center` in a 100 px area, `Layout.Bounds.Width` is 100 while `Size.Width` is the width of the text. Laying the same text out again at a width equal to `Size.Width` breaks the same lines, so measure, arrange and render agree. `Size.Height` is the height of the layout.

`LineCount` is the number of lines in `Layout`. A paragraph with no spans has `Size` zero and `LineCount` `0`; a paragraph with one empty span has one line with width `0`.

The runs in `Layout` carry a placeholder brush. `TextRenderer.RenderParagraph` takes the real brush from the `TextSpan` that each run came from, so a layout is shared by paragraphs that differ only in brush or decorations.

The constructor is public but is used by `TextMeasurer`; callers normally get an instance from `MeasureParagraph`.

## Constructors

| Name | Description |
| --- | --- |
| `TextParagraphLayout(LayoutSize size, int lineCount, DrawTextLayout layout, string renderIdentity)` | Creates a layout. `size` is clamped to non-negative values. Throws `ArgumentOutOfRangeException` when `lineCount` is negative, `ArgumentNullException` when `layout` is `null`, and `ArgumentException` when `renderIdentity` is empty or white space. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Size` | `LayoutSize` | Gets the measured size: the width of the widest line and the height of the layout. |
| `LineCount` | `int` | Gets the number of laid out lines. |
| `Layout` | `DrawTextLayout` | Gets the Drawing engine result with the lines and runs. |

## Applies to

`TextMeasurer.MeasureParagraph` and `TextRenderer.RenderParagraph`.

## See also

- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.TextRenderer`
- `Cerneala.UI.Text.TextSpan`
- `Cerneala.UI.Text.TextParagraphOptions`
- `Cerneala.Drawing.DrawTextLayout`
