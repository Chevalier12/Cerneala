# LineBreakService Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/LineBreakService.cs`

Breaks text into `TextLine` values using a simple width estimate, paragraph splitting, and text-element-aware wrapping.

```csharp
public sealed class LineBreakService
```

Inheritance:
`Object` -> `LineBreakService`

## Examples
The following example wraps text using the shared service instance and a `TextAspect`.

```csharp
using Cerneala.UI.Text;

TextAspect aspect = new(
    "Arial",
    16,
    TextWrapping.NoWrap,
    TextTrimming.WordEllipsis);
ResolvedTextFont font = FontResolver.Default.Resolve(aspect);

IReadOnlyList<TextLine> lines = LineBreakService.Default.BreakLines(
    "alpha beta gamma",
    aspect,
    font,
    availableWidth: 48);

float measuredWidth = LineBreakService.Default.MeasureTextWidth("alpha", aspect);
```

## Remarks
`LineBreakService` is a stateless helper used by text measurement and rendering code. Use `Default` when a shared instance is enough, or create a new instance when a caller wants its own service object.

`BreakLines` first splits input into paragraphs at `\r`, `\n`, and `\r\n` separators. The separator characters are not included in the returned `TextLine.Text` values. An empty input string returns one empty `TextLine` with width `0`.

Wrapping is skipped when `TextAspect.Wrapping` is `TextWrapping.NoWrap`, when `availableWidth` is positive infinity, or when `availableWidth` is less than or equal to `0`. In those cases each paragraph is returned as a single line.

When wrapping is enabled, the service parses the paragraph with `StringInfo.ParseCombiningCharacters`, so fallback breaks are made at text element boundaries rather than inside combining-character sequences. Preferred break opportunities occur after whitespace and after `-`, `/`, `\`, `,`, `;`, or `:`. Whitespace used as a wrap boundary is trimmed from the end of the emitted line, and leading break whitespace is skipped before the next line.

When trimming is `CharacterEllipsis` or `WordEllipsis`, finite-width overflowing lines are collapsed with a `U+2026` glyph. Character trimming uses the closest fitting Unicode text-element boundary. Word trimming uses the closest complete break boundary and falls back to character trimming when the first word cannot fit. If even the ellipsis glyph cannot fit, the service returns an empty line.

The `BreakLines` overload receives a resolved font and uses `TextShaper` advances when available, with the fixed `aspect.FontSize * aspect.Scale * 0.5f` estimate as a fallback. The public `MeasureTextWidth(string, TextAspect)` convenience method uses the fixed estimate because it does not receive a resolved font.

Passing `null` for `text` to `BreakLines` or `MeasureTextWidth` throws `ArgumentNullException`.

## Constructors
| Name | Description |
| --- | --- |
| `LineBreakService()` | Initializes a new instance of the `LineBreakService` class. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Default` | `LineBreakService` | Gets a shared default line break service instance. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `BreakLines(string text, TextAspect aspect, ResolvedTextFont font, float availableWidth)` | `IReadOnlyList<TextLine>` | Splits and, when requested, ellipsizes `text` according to paragraphs, wrapping, trimming, and available width. |
| `MeasureTextWidth(string text, TextAspect aspect)` | `float` | Returns the estimated width of `text` using the service's fixed character-width formula. |

## Related Supporting Types
| Name | Description |
| --- | --- |
| `TextAspect` | Provides font size, scale, wrapping, trimming, color, and font resource information used by text services. |
| `TextLine` | Immutable record struct containing the emitted line text and estimated width. |
| `TextWrapping` | Enum with `NoWrap` and `Wrap` values. |

## Applies to
Project: `Cerneala`

## See also
- `TextMeasurer`
- `TextAspect`
- `TextLine`
- `TextWrapping`
