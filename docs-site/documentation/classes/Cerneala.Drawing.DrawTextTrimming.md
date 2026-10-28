# DrawTextTrimming Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Selects ellipsis behavior when constrained text is omitted.

```csharp
public enum DrawTextTrimming
```

## Examples

```csharp
DrawTextLayoutOptions options = new(maxWidth: 180, maxLines: 2, trimming: DrawTextTrimming.WordEllipsis);
```

## Remarks

Both ellipsis modes preserve Unicode grapheme clusters. Trimming is applied for width overflow or when `MaxLines`/`MaxHeight` omits remaining content. As in WPF, every line wider than `MaxWidth` is collapsed, not only the last one: `"abcdefghij\nab"` with `NoWrap`, `CharacterEllipsis`, and `MaxWidth` 20 at 5 px per character gives the lines `"abc…"` and `"ab"`. The last visible line also gets the ellipsis when `MaxLines` or `MaxHeight` omits the lines after it.

The ellipsis has the style of the span of the last kept character (or of the span at the start of the line when no character is kept), and trimming measures it with that same span, so a trimmed line never exceeds `MaxWidth` because of the ellipsis. Example: the spans `"bbbb"` at size 20 (10 px per character) and `"aaaa"` at size 10 (5 px per character) with `MaxWidth` 45 give `"bbb…"`, 30 + 10 = 40 px, because `"bbbb…"` would be 50 px.

`WordEllipsis` counts the end of a line as a word boundary when white space, a line break, or the end of the text follows it. So `"aa bb\ncc"` with `MaxLines` 1 gives `"aa bb…"`. The end of a line that `Character` wrapping broke inside a word is not a word boundary.

## Values

| Name | Description |
| --- | --- |
| `None` | Does not append an ellipsis. |
| `CharacterEllipsis` | Keeps the largest fitting cluster prefix and appends `…`. |
| `WordEllipsis` | Prefers the last fitting word boundary and appends `…`. |

## Applies To

`DrawTextLayoutOptions.Trimming`.
