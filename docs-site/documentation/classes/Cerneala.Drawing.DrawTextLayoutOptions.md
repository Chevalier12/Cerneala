# DrawTextLayoutOptions Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Defines immutable text constraints, flow, alignment, trimming, direction, scale, line height, and line stacking.

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
    scale: 1,
    lineHeight: 24,
    lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight);
```

## Remarks

Positive infinity means unconstrained width or height. `MaxLines` equal to zero means unlimited. `Scale` participates in the layout cache key and scales shaping, measurement, and line metrics.

`LineHeight` is `float.NaN` by default, which keeps the height of the content of each line. A finite value greater than zero sets the height of the line box, and `LineStackingStrategy` says how: `MaxHeight` (default) uses the larger of `LineHeight` and the content height, and `BlockLineHeight` uses exactly `LineHeight`. The baseline moves in proportion with the line box. `LineSpacing` then multiplies the line box height to get the distance between lines, and `MaxHeight` is compared with the sum of those distances. For example, with `lineHeight: 24`, `BlockLineHeight`, and `lineSpacing: 1.5`, every line advances 36. Both new options are part of the layout cache key, so layouts that differ in them are never shared.

`Wrapping` can be `DrawTextWrapping.WordWithOverflow` and `Alignment` can be the physical `DrawTextAlignment.Left` and `Right`. For example, `new DrawTextLayoutOptions(maxWidth: 40, alignment: DrawTextAlignment.Right)` puts a 10 px wide line at x = 30 for left-to-right and for right-to-left text alike.

## Constructors

| Name | Description |
| --- | --- |
| `DrawTextLayoutOptions(float, float, DrawTextWrapping, DrawTextAlignment, float, int, DrawTextTrimming, DrawTextDirection, float, float, DrawLineStackingStrategy)` | Creates validated immutable layout options. The defaults are `maxWidth = float.PositiveInfinity`, `maxHeight = float.PositiveInfinity`, `wrapping = NoWrap`, `alignment = Start`, `lineSpacing = 1`, `maxLines = 0` (no line limit), `trimming = None`, `direction = Auto`, `scale = 1`, `lineHeight = float.NaN` (content height), and `lineStackingStrategy = MaxHeight`. |

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
| `LineHeight` | `float` | Gets the line box height, or `float.NaN` to keep the height of the line content. |
| `LineStackingStrategy` | `DrawLineStackingStrategy` | Gets how `LineHeight` combines with the content height. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | `maxWidth` or `maxHeight` is negative or `NaN` (`float.PositiveInfinity` is allowed); `wrapping`, `alignment`, `trimming`, or `direction` is not a defined enum value; `lineSpacing` is not finite or is less than or equal to `0`; `maxLines` is negative; `scale` is not finite or is less than or equal to `0`; `lineHeight` is not `NaN` and is not finite or is less than or equal to `0`; or `lineStackingStrategy` is not a defined enum value. |

## Applies To

Layout building, caching, measurement, and retained drawing.

## See Also

- `Cerneala.Drawing.DrawLineStackingStrategy`
- `Cerneala.Drawing.DrawTextWrapping`
- `Cerneala.Drawing.DrawTextAlignment`
