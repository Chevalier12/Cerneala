# DrawTextAlignment Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Selects horizontal placement inside a constrained text layout.

```csharp
public enum DrawTextAlignment
```

## Examples

```csharp
DrawTextLayoutOptions options = new(maxWidth: 320, alignment: DrawTextAlignment.Center);
```

## Remarks

`Start` and `End` follow the resolved line direction: start is left for LTR and right for RTL. `Left` and `Right` do not depend on the direction: with a 40 px width constraint, a 10 px line starts at x = 0 for `Left` and at x = 30 for `Right`. Justification expands inter-word whitespace on untrimmed lines. It never spreads the last line of a paragraph, meaning the line that ends the text or comes before a hard line break.

A line of a justified paragraph that is not spread (the last line of a paragraph, a trimmed line, or a line with no whitespace to widen) is placed like `Start`: at the left edge of a `LeftToRight` line and at the right edge of a `RightToLeft` line. For example, a 10 px last line in a 30 px wide right-to-left paragraph starts at x = 20; in a left-to-right paragraph it starts at x = 0.

Justification changes the width of the whitespace, not the shapes of the letters. On a spread line every whitespace element is a run of its own and takes an equal share of the remaining width, and the letters between two whitespace elements stay one run when they share a span, a font, and a bidi embedding level (Unicode UAX #9). So the shaper shapes a word together, as on a `Start` line, and Arabic letters keep their joined forms.

## Values

| Name | Description |
| --- | --- |
| `Start` | Aligns with the direction-aware leading edge. |
| `Center` | Centers line content. |
| `End` | Aligns with the direction-aware trailing edge. |
| `Justify` | Distributes remaining width across eligible whitespace. |
| `Left` | Aligns every line with the physical left edge, for left-to-right and right-to-left text alike. |
| `Right` | Aligns every line with the physical right edge, for left-to-right and right-to-left text alike. |

## Applies To

`DrawTextLayoutOptions.Alignment`.
