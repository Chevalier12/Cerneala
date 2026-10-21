# TextAlignment Enum

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextAlignment.cs`

Specifies how the lines of a paragraph are placed horizontally inside the width of the text.

```csharp
public enum TextAlignment
```

## Examples

Center a short title in a 200 px wide `TextBlock`, in C#:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBlock title = new()
{
    Text = "Settings",
    TextAlignment = TextAlignment.Center,
    Width = 200
};
```

The same in markup:

```xml
<TextBlock TextAlignment="Center" Text="Settings" Width="200"/>
```

If the text `Settings` is 60 px wide, it starts 70 px from the left edge of the 200 px area (`(200 - 60) / 2`). With `Right` it starts 140 px from the left edge, and with `Left` at 0.

## Remarks

The values are physical, as in WPF: `Left` and `Right` do not depend on the text direction. Right-to-left text with `Left` still starts at the left edge.

The alignment uses the width that the `TextBlock` is arranged at. It does not change the measured width: a `TextBlock` with `TextAlignment.Center` measures to the same size as one with `Left`.

`Justify` spreads the extra width over the white space of a line, so the line reaches both edges. It never spreads the last line of a paragraph, that is a line that ends the text or that ends before a hard line break (`\n` in the text, or a `LineBreak` inline). It does not spread a line that is trimmed with an ellipsis either. A decoration such as an underline stays continuous over the added space.

`TextAlignment` is the `Cerneala.UI.Text` counterpart of `Cerneala.Drawing.DrawTextAlignment`. `TextMeasurer` maps `Left` to `DrawTextAlignment.Left`, `Right` to `Right`, `Center` to `Center` and `Justify` to `Justify`.

Validation: `TextBlock.TextAlignment` throws `ArgumentException` for a value outside the enum, and `TextParagraphOptions` throws `ArgumentOutOfRangeException`.

In `.crn` markup the value is the member name, matched without regard to case.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Left` | `0` | Lines start at the left edge. This is the default. |
| `Right` | `1` | Lines end at the right edge. |
| `Center` | `2` | Lines are centered. |
| `Justify` | `3` | Lines that do not end a paragraph are stretched to the full width. |

## Applies to

`TextBlock.TextAlignment` and `TextParagraphOptions.Alignment`.

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Text.TextParagraphOptions`
- `Cerneala.Drawing.DrawTextAlignment`
