# LineStackingStrategy Enum

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/LineStackingStrategy.cs`

Specifies how `LineHeight` sets the height of each line of a paragraph.

```csharp
public enum LineStackingStrategy
```

## Examples

Three lines of text with a line height of 40, in C#:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBlock lines = new()
{
    Text = "a\nb\nc",
    LineHeight = 40,
    LineStackingStrategy = LineStackingStrategy.BlockLineHeight
};
// Desired height: 3 * 40 = 120.
```

The same in markup:

```xml
<TextBlock LineHeight="40" LineStackingStrategy="BlockLineHeight">a<LineBreak/>b<LineBreak/>c</TextBlock>
```

With `LineHeight = 5`, the height depends on the strategy. If one line of the font is 20 px high:

| Strategy | Height of 3 lines |
| --- | ---: |
| `MaxHeight` | 60 (each line keeps its content height of 20, because it is larger than 5) |
| `BlockLineHeight` | 15 (each line is exactly 5) |

## Remarks

The strategy matters only when `TextBlock.LineHeight` has a value. With the default `NaN`, every line is as high as its content.

The baseline moves with the line: when a line box is higher or lower than its content, the text keeps its place in proportion inside the line box.

`LineStackingStrategy` is the `Cerneala.UI.Text` counterpart of `Cerneala.Drawing.DrawLineStackingStrategy`.

Validation: `TextBlock.LineStackingStrategy` throws `ArgumentException` for a value outside the enum, and `TextParagraphOptions` throws `ArgumentOutOfRangeException`.

In `.crn` markup the value is the member name, matched without regard to case.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `MaxHeight` | `0` | A line is as high as the larger of its content and `LineHeight`. This is the default. |
| `BlockLineHeight` | `1` | A line is exactly `LineHeight` high. |

## Applies to

`TextBlock.LineStackingStrategy` and `TextParagraphOptions.LineStackingStrategy`.

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Text.TextParagraphOptions`
- `Cerneala.Drawing.DrawLineStackingStrategy`
