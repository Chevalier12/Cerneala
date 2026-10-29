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

`Auto` resolves the base direction once per paragraph, from the first text element of the paragraph that is a letter or a decimal digit. A paragraph is the text between hard line breaks (`\r`, `\n`, or `\r\n`), and every wrapped line of a paragraph gets the direction of its paragraph. A letter or digit in a right-to-left Unicode block (for example Hebrew or Arabic, including the Arabic-Indic digits `٠` to `٩`) gives `RightToLeft`; any other letter or decimal digit gives `LeftToRight`. So `"123 שלום"` resolves to `LeftToRight`, because the ASCII digit `1` comes first, and `"١٢٣"` and `"٣ שלום"` resolve to `RightToLeft`. A paragraph with no letter or digit resolves to `LeftToRight`. For example, `"אבג דהו abc def זחט"` gives `RightToLeft` for every wrapped line, also for a line that holds only `abc def`, while `"שלום\nabc"` gives a `RightToLeft` line and then a `LeftToRight` line. Direction-aware alignment and bidi visual run order use the resolved value.

The direction of each run comes from the implicit rules of the Unicode Bidirectional Algorithm (UAX #9); explicit embedding, override, and isolate characters are not supported. A number does not split the right-to-left text around it, and its digits keep their left-to-right order, with ASCII digits and with Arabic-Indic digits alike. So `"אב 12 גד"` is drawn `דג 12 בא` from left to right in a `LeftToRight` and in a `RightToLeft` line, and in `"אב ١٢٣ גד"` the number is a `LeftToRight` run that is drawn `١٢٣`.

## Values

| Name | Description |
| --- | --- |
| `Auto` | Resolves direction from the text. |
| `LeftToRight` | Forces an LTR base direction. |
| `RightToLeft` | Forces an RTL base direction. |

## Applies To

Text layout options, lines, and positioned runs.
