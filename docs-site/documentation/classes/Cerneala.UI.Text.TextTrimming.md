# TextTrimming Enum

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextTrimming.cs`

Specifies how text is collapsed when it overflows its available layout bounds.

```csharp
public enum TextTrimming
```

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBlock label = new()
{
    Text = "A long label that must remain inside its bounds",
    Width = 140,
    TextWrapping = TextWrapping.NoWrap,
    TextTrimming = TextTrimming.WordEllipsis
};
```

## Remarks

`CharacterEllipsis` keeps the longest prefix that fits at a Unicode text-element boundary and appends the `U+2026` ellipsis glyph. When the available width is zero, negative, or not finite, or narrower than the ellipsis itself, both ellipsis modes produce an empty line without an ellipsis. It does not split surrogate pairs or combining-character sequences.

`WordEllipsis` prefers the closest complete word or line-break boundary that fits with the ellipsis. The end of a line is such a boundary when white space, a line break, or the end of the text follows it: `"Hello world\nSecond line"` with `MaxLines = 1` shows `"Hello world…"`. When the first word is wider than the available line, it falls back to character-boundary trimming so the line can still show useful content.

As in WPF, every line that is wider than the available width is collapsed, not only the last one. With `TextWrapping.NoWrap`, `"abcdefghij\nab"` in a 20 px width at 5 px per character shows `"abc…"` and `"ab"`. With finite height and multiple lines, either ellipsis mode keeps the lines that fit, always preserves at least the first line, and collapses the final visible line to indicate hidden content. `None` leaves overflowing text uncollapsed.

In a `TextBlock` with inlines of different sizes, the ellipsis has the font and size of the last kept character, and trimming measures it with that same style, so the trimmed line fits the available width.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | Does not collapse overflowing text. |
| `CharacterEllipsis` | `1` | Collapses at a Unicode text-element boundary and appends an ellipsis. |
| `WordEllipsis` | `2` | Collapses at a complete word boundary when possible and appends an ellipsis. |

## Applies to

Cerneala text layout, `TextBlock`, and APIs that consume `TextAspect`.

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Text.TextWrapping`
