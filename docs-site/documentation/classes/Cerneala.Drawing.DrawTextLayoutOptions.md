# DrawTextLayoutOptions Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Defines immutable text constraints, flow, alignment, trimming, direction, and scale.

```csharp
public sealed record DrawTextLayoutOptions
```

## Examples

```csharp
DrawTextLayoutOptions options = new(
    maxWidth: 320,
    maxHeight: 120,
    wrapping: DrawTextWrapping.Word,
    alignment: DrawTextAlignment.Start,
    lineSpacing: 1.15f,
    maxLines: 4,
    trimming: DrawTextTrimming.WordEllipsis,
    direction: DrawTextDirection.Auto,
    scale: 1);
```

## Remarks

Positive infinity means unconstrained width or height. `MaxLines` equal to zero means unlimited. `Scale` participates in the layout cache key and scales shaping, measurement, and line metrics.

## Constructors

| Name | Description |
| --- | --- |
| `DrawTextLayoutOptions(float, float, DrawTextWrapping, DrawTextAlignment, float, int, DrawTextTrimming, DrawTextDirection, float)` | Creates validated immutable layout options. The defaults are `maxWidth = float.PositiveInfinity`, `maxHeight = float.PositiveInfinity`, `wrapping = NoWrap`, `alignment = Start`, `lineSpacing = 1`, `maxLines = 0` (no line limit), `trimming = None`, `direction = Auto`, and `scale = 1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `MaxWidth` | `float` | Gets the non-negative width constraint. |
| `MaxHeight` | `float` | Gets the non-negative height constraint. |
| `Wrapping` | `DrawTextWrapping` | Gets line wrapping behavior. |
| `Alignment` | `DrawTextAlignment` | Gets horizontal alignment. |
| `LineSpacing` | `float` | Gets the positive line-height multiplier. |
| `MaxLines` | `int` | Gets the maximum line count, or zero for unlimited. |
| `Trimming` | `DrawTextTrimming` | Gets ellipsis behavior. |
| `Direction` | `DrawTextDirection` | Gets the requested base direction. |
| `Scale` | `float` | Gets the positive layout scale. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | `maxWidth` or `maxHeight` is negative or `NaN` (`float.PositiveInfinity` is allowed); `wrapping`, `alignment`, `trimming`, or `direction` is not a defined enum value; `lineSpacing` is not finite or is less than or equal to `0`; `maxLines` is negative; or `scale` is not finite or is less than or equal to `0`. |

## Applies To

Layout building, caching, measurement, and retained drawing.
