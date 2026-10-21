# DrawLineStackingStrategy Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawTextLayout.cs`

Selects how `DrawTextLayoutOptions.LineHeight` sets the height of each line box in a text layout.

```csharp
public enum DrawLineStackingStrategy
```

## Examples

For lines whose content is 12 px high, a requested line height gives these line boxes:

```csharp
using Cerneala.Drawing;

// MaxHeight: the line box is the larger of 40 and the content height (12), so 40.
DrawTextLayoutOptions atLeast = new(lineHeight: 40);

// BlockLineHeight: the line box is exactly 40, also when the content is higher.
DrawTextLayoutOptions exactly = new(
    lineHeight: 40,
    lineStackingStrategy: DrawLineStackingStrategy.BlockLineHeight);

// With lineHeight: 6, MaxHeight still gives 12 (the content height); BlockLineHeight gives 6.
```

## Remarks

The strategy only matters when `DrawTextLayoutOptions.LineHeight` is not `NaN`. With the default `NaN`, every line is as high as its content, whatever the strategy.

The content height of a line comes from every span on the line: the line baseline is the largest baseline of its spans and the height is that baseline plus the largest part below a baseline. When the line box height differs from the content height, the baseline moves with the line box in proportion: for a content height of 12 and a baseline of 9.6, a `BlockLineHeight` of 40 puts the baseline at 32 and a `BlockLineHeight` of 6 puts it at 4.8.

`DrawTextLayoutOptions.LineSpacing` multiplies the line box height after the strategy is applied, to get the distance from one line to the next.

`TextBlock` passes `LineStackingStrategy` to the layout through `TextMeasurer.MeasureParagraph`.

## Values

| Name | Description |
| --- | --- |
| `MaxHeight` | A line box is as high as the larger of `LineHeight` and the content of the line. This is the default. |
| `BlockLineHeight` | A line box is exactly `LineHeight` high, also when that is smaller than its content. |

## Applies to

`DrawTextLayoutOptions.LineStackingStrategy`.

## See also

- `Cerneala.Drawing.DrawTextLayoutOptions`
- `Cerneala.UI.Text.LineStackingStrategy`
- `Cerneala.UI.Controls.TextBlock`
