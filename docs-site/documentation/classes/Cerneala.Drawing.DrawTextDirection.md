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

## Values

| Name | Description |
| --- | --- |
| `Auto` | Resolves direction from the text. |
| `LeftToRight` | Forces an LTR base direction. |
| `RightToLeft` | Forces an RTL base direction. |

## Applies To

Text layout options, lines, and positioned runs.
