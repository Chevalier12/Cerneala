# TextParagraphOptions Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextParagraphOptions.cs`

Holds the paragraph-level layout options of a `TextBlock`: alignment, the most lines, the line height, and the line stacking strategy.

```csharp
public sealed record TextParagraphOptions
```

Inheritance:
`object` -> `TextParagraphOptions`

## Examples

Center the lines, show at most 3 of them, and make every line 24 high:

```csharp
using Cerneala.UI.Text;

TextParagraphOptions options = new(
    Alignment: TextAlignment.Center,
    MaxLines: 3,
    LineHeight: 24,
    LineStackingStrategy: LineStackingStrategy.BlockLineHeight);
```

Options with the same values are equal:

```csharp
using Cerneala.UI.Text;

bool same = new TextParagraphOptions(TextAlignment.Center, 2, 30) ==
    new TextParagraphOptions(TextAlignment.Center, 2, 30);
// true

bool other = new TextParagraphOptions(TextAlignment.Center, 3, 30) ==
    new TextParagraphOptions(TextAlignment.Center, 2, 30);
// false
```

## Remarks

The options are validated when they are created, and they never change after that. The wrapping, trimming and scale of the paragraph are not here: they come from the paragraph `TextAspect` that `TextMeasurer.MeasureParagraph` and `TextRenderer.RenderParagraph` receive next to these options.

`TextBlock` creates one `TextParagraphOptions` from its `TextAlignment`, `MaxLines`, `LineHeight` and `LineStackingStrategy` each time it measures or renders.

The options are part of the paragraph layout cache key, so two paragraphs with different options never share a cached layout.

## Constructors

| Name | Description |
| --- | --- |
| `TextParagraphOptions(TextAlignment Alignment = TextAlignment.Left, int MaxLines = 0, float LineHeight = float.NaN, LineStackingStrategy LineStackingStrategy = LineStackingStrategy.MaxHeight)` | Creates validated options. Throws `ArgumentOutOfRangeException` when `Alignment` or `LineStackingStrategy` is not a defined value, when `MaxLines` is negative, or when `LineHeight` is not `NaN` and is zero, negative, or infinite. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Alignment` | `TextAlignment` | Gets how the lines are placed horizontally. The default is `Left`. |
| `MaxLines` | `int` | Gets the most lines that are shown. `0` means no limit. |
| `LineHeight` | `float` | Gets the height of a line box. `NaN` means automatic: the height of the content of the line. |
| `LineStackingStrategy` | `LineStackingStrategy` | Gets how `LineHeight` is applied. The default is `MaxHeight`. |

## Applies to

`TextMeasurer.MeasureParagraph` and `TextRenderer.RenderParagraph`.

## See also

- `Cerneala.UI.Text.TextAlignment`
- `Cerneala.UI.Text.LineStackingStrategy`
- `Cerneala.UI.Text.TextSpan`
- `Cerneala.UI.Text.TextParagraphLayout`
- `Cerneala.UI.Controls.TextBlock`
