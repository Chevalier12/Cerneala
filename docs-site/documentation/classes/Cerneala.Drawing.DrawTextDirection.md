# DrawTextDirection Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Controls paragraph base direction or reports a resolved run direction.

```csharp
public enum DrawTextDirection
```

## Examples

```csharp
DrawTextLayoutOptions options = new(direction: DrawTextDirection.Auto);
```

## Remarks

`Auto` resolves the base direction from the first text element that is a letter or a decimal digit. A letter or digit in a right-to-left Unicode block (for example Hebrew or Arabic) gives `RightToLeft`; any other letter or decimal digit gives `LeftToRight`. So `"123 שלום"` resolves to `LeftToRight`, because the ASCII digit `1` comes first. Text with no letter or digit resolves to `LeftToRight`. Direction-aware alignment and bidi visual run order use the resolved value.

A neutral, such as a space or a punctuation mark, takes a direction from its neighbors as in Unicode UAX #9 rules N1 and N2. Neutrals between two strong characters of the same direction take that direction; neutrals between strong characters of different directions, or at the start or the end of the line, take the base direction. So in a `LeftToRight` line, `"abcd שלום efgh"` has the runs `"abcd "` (`LeftToRight`), `"שלום"` (`RightToLeft`), and `" efgh"` (`LeftToRight`), and is drawn as `abcd םולש efgh`, with one space on each side of the Hebrew word. `"שלום עולם"` stays one `RightToLeft` run, because its space lies between two right-to-left letters. A lone UTF-16 surrogate is neutral.

## Values

| Name | Description |
| --- | --- |
| `Auto` | Resolves direction from the text. |
| `LeftToRight` | Forces an LTR base direction. |
| `RightToLeft` | Forces an RTL base direction. |

## Applies To

Text layout options, lines, and positioned runs.
