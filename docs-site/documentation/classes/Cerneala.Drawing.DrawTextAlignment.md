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
