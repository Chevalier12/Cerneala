# DrawTextWrapping Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Selects how a text layout creates lines at a width constraint.

```csharp
public enum DrawTextWrapping
```

## Examples

```csharp
DrawTextLayoutOptions options = new(maxWidth: 240, wrapping: DrawTextWrapping.Word);
```

## Remarks

Wrapping operates on Unicode grapheme clusters. `Word` prefers shared line-break opportunities and falls back to cluster boundaries for an overlong word; `Character` may break at every cluster.

## Values

| Name | Description |
| --- | --- |
| `NoWrap` | Keeps each explicit paragraph on one line. |
| `Word` | Prefers word and punctuation boundaries, then falls back safely. |
| `Character` | Wraps at Unicode grapheme-cluster boundaries. |

## Applies To

`DrawTextLayoutOptions.Wrapping`.
