# LineBreakService.TextElement Record

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/LineBreakService.cs`

Represents one parsed text element in a paragraph while `LineBreakService` computes wrap boundaries.

```csharp
private readonly record struct TextElement(int Start, int End, string Text);
```

Containing type:
`Cerneala.UI.Text.LineBreakService`

## Examples
`TextElement` is private to `LineBreakService`; callers use `BreakLines` instead of constructing text elements directly.

```csharp
using Cerneala.UI.Text;

TextAspect aspect = new("Arial", 16, TextWrapping.Wrap);

IReadOnlyList<TextLine> lines = LineBreakService.Default.BreakLines(
    "Cafe\u0301 au lait",
    aspect,
    availableWidth: 48);
```

## Remarks
`TextElement` is an implementation detail used during wrapping. `LineBreakService` creates an array of these values from `StringInfo.ParseCombiningCharacters`, so fallback line breaks advance by text element boundaries rather than splitting combining-character sequences.

Each value stores the source paragraph start index, exclusive end index, and substring for one text element. The wrapping loop reads `Start` and `End` to measure candidate line spans, and reads `Text` to detect whitespace and punctuation break opportunities.

Because the type is private, application code should use `LineBreakService.BreakLines`, `TextLine`, and `TextAspect` rather than depending on this nested record.

## Constructors
| Name | Description |
| --- | --- |
| `TextElement(int, int, string)` | Initializes a text element with source indexes and the corresponding text substring. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Start` | `int` | Gets the zero-based start index of the text element in the source paragraph. |
| `End` | `int` | Gets the exclusive end index of the text element in the source paragraph. |
| `Text` | `string` | Gets the substring represented by the text element. |

## Applies to
Cerneala retained UI text wrapping internals.

## See also
- `Cerneala.UI.Text.LineBreakService`
- `Cerneala.UI.Text.TextLine`
- `Cerneala.UI.Text.TextAspect`
