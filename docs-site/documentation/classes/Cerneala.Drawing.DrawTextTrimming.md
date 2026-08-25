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

Both ellipsis modes preserve Unicode grapheme clusters. Trimming is applied for width overflow or when `MaxLines`/`MaxHeight` omits remaining content.

## Values

| Name | Description |
| --- | --- |
| `None` | Does not append an ellipsis. |
| `CharacterEllipsis` | Keeps the largest fitting cluster prefix and appends `…`. |
| `WordEllipsis` | Prefers the last fitting word boundary and appends `…`. |

## Applies To

`DrawTextLayoutOptions.Trimming`.
