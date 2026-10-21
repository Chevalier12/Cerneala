# TextDecorations Enum

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextDecorations.cs`

Selects the lines that are drawn on text: underline, strikethrough, overline, and baseline. The members are bit flags and can be combined.

```csharp
[Flags]
public enum TextDecorations
```

Attributes:
`FlagsAttribute`

## Examples

Underline and strike through the whole `TextBlock`, in C#:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBlock price = new()
{
    Text = "Old price",
    TextDecorations = TextDecorations.Underline | TextDecorations.Strikethrough
};
```

The same in markup. A combination is written as member names separated by commas:

```xml
<TextBlock TextDecorations="Underline, Strikethrough" Text="Old price"/>
```

Underline only one word, with a `Span` (the other text has no decoration):

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;
using Cerneala.UI.Text;

TextBlock text = new();
text.Inlines.Add(new Run("Press "));
text.Inlines.Add(new Run("Enter") { TextDecorations = TextDecorations.Underline });
```

## Remarks

Every decoration is drawn as one `FillRectangle` command in the foreground brush of the span, after the `DrawText` command of the same run. A decoration spans the width of the run, so with `TextAlignment.Justify` it stays continuous over the added space.

Where each line is drawn, for a run with its baseline at `y`:

| Flag | Rectangle |
| --- | --- |
| `Underline` | Top at `y + UnderlineOffset`, height `UnderlineThickness`. |
| `Strikethrough` | Bottom at `y + StrikethroughOffset`, height `StrikethroughThickness`. The offset is normally negative, so the stroke is above the baseline. |
| `Overline` | Top at the top of the line box, height `UnderlineThickness`. |
| `Baseline` | Top at `y`, height `UnderlineThickness`. |

The offsets and thicknesses come from `TextDecorationMetrics`.

Decorations add up. A decoration on a `TextBlock`, on a `Span` and on a `Run` all apply to the text inside, and an explicit `None` on a child does not remove the decoration of its parent. For example, a `Span` with `Underline` that holds a `Run` with `Strikethrough` draws both lines on that run, and a `Run` with `None` in the same `Span` is still underlined.

A decoration changes only the render, not the measure.

A value that has a bit outside the four flags, such as `(TextDecorations)16`, is rejected: `TextBlock.TextDecorations` and `Inline.TextDecorations` throw `ArgumentException`.

In `.crn` markup the value is member names, matched without regard to case, separated by commas.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | No decoration. This is the default. |
| `Underline` | `1` | A line under the baseline. |
| `Strikethrough` | `2` | A line above the baseline, through the text. |
| `Overline` | `4` | A line at the top of the line box. |
| `Baseline` | `8` | A line on the baseline. |

## Applies to

`TextBlock.TextDecorations`, `Inline.TextDecorations` and `TextSpan.Decorations`.

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Documents.Inline`
- `Cerneala.UI.Text.TextSpan`
- `Cerneala.Drawing.Text.TextDecorationMetrics`
