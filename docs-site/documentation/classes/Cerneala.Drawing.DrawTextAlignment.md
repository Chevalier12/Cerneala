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

`Start` and `End` follow the resolved line direction: start is left for LTR and right for RTL. Justification expands inter-word whitespace on non-final, untrimmed lines.

## Values

| Name | Description |
| --- | --- |
| `Start` | Aligns with the direction-aware leading edge. |
| `Center` | Centers line content. |
| `End` | Aligns with the direction-aware trailing edge. |
| `Justify` | Distributes remaining width across eligible whitespace. |

## Applies To

`DrawTextLayoutOptions.Alignment`.
