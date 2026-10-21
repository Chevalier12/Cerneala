# TextSpan Struct

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextSpan.cs`

Describes one piece of paragraph text with one style: its text, its `TextAspect`, and its text decorations.

```csharp
public readonly record struct TextSpan(string Text, TextAspect Aspect, TextDecorations Decorations = TextDecorations.None)
```

Inheritance:
`ValueType` -> `TextSpan`

## Examples

Lay out one paragraph from a regular span and a bold span, and measure it:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

SolidColorBrush black = new(Color.Black);
TextAspect regular = new("Segoe UI", 16, foreground: black);
TextAspect bold = new("Segoe UI", 16, foreground: black, fontWeight: FontWeight.Bold);

TextSpan[] spans =
[
    new TextSpan("Hello ", regular),
    new TextSpan("world", bold, TextDecorations.Underline)
];

TextParagraphLayout paragraph = TextMeasurer.Default.MeasureParagraph(
    spans,
    regular,
    new TextParagraphOptions(),
    new LayoutSize(float.PositiveInfinity, float.PositiveInfinity));
```

## Remarks

A `TextBlock` creates one `TextSpan` for each piece of text of its inline tree, depth first, with the style that its `Run`, `Span`, `Bold` and `Italic` elements give that piece. When it has no text, it creates one empty span with its own style.

The spans of a paragraph are laid out as one text: a line can hold several spans, and a line break opportunity can fall in one span or between two. The `Aspect` of a span gives its font family, size, weight, style and stretch. The wrapping, trimming and scale of the layout come from the paragraph `TextAspect` that is passed next to the spans, not from the aspect of each span.

`Aspect.Foreground` is the brush that `TextRenderer.RenderParagraph` draws the span with, for its text and for its decorations. A span whose `Aspect.Foreground` is `null` draws nothing, neither text nor decorations. The brush, the foreground and the decorations are not part of the layout: two paragraphs that differ only in them share one cached layout.

`Decorations` draw over the whole span. See `TextDecorations`.

## Constructors

| Name | Description |
| --- | --- |
| `TextSpan(string Text, TextAspect Aspect, TextDecorations Decorations = TextDecorations.None)` | Creates a span. Performs no validation. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the text of the span. |
| `Aspect` | `TextAspect` | Gets the font, size and foreground of the span. |
| `Decorations` | `TextDecorations` | Gets the decorations drawn over the span. The default is `None`. |

## Applies to

`TextMeasurer.MeasureParagraph` and `TextRenderer.RenderParagraph`.

## See also

- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Text.TextDecorations`
- `Cerneala.UI.Text.TextParagraphOptions`
- `Cerneala.UI.Text.TextParagraphLayout`
- `Cerneala.UI.Text.TextMeasurer`
