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

`Auto` resolves the first strong Unicode direction. Direction-aware alignment and bidi visual run order use the resolved value.

## Values

| Name | Description |
| --- | --- |
| `Auto` | Resolves direction from the text. |
| `LeftToRight` | Forces an LTR base direction. |
| `RightToLeft` | Forces an RTL base direction. |

## Applies To

Text layout options, lines, and positioned runs.
