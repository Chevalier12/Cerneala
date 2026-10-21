# TextWrapping Enum

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextWrapping.cs`

Specifies whether text breaks onto more lines when it is wider than the available width.

```csharp
public enum TextWrapping
```

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBlock text = new();
Console.WriteLine(text.TextWrapping);
// NoWrap

text.TextWrapping = TextWrapping.Wrap;
Console.WriteLine(text.TextWrapping);
// Wrap

try
{
    text.TextWrapping = (TextWrapping)5;
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}
// Value for 'Cerneala.UI.Controls.TextBlock.TextWrapping' failed validation. (Parameter 'value')
```

## Remarks

`TextBlock.TextWrapping` defaults to `NoWrap` and invalidates measure and render when it changes. The value reaches the line breaker through `TextAspect.Wrapping`:

| Value | Lines |
| --- | --- |
| `NoWrap` | One line per paragraph. A line break in the text still starts a new line. |
| `Wrap` | Breaks after the last break opportunity, such as a space, that fits the width. A word wider than the width is split after the last character that fits. If not even one character fits, that character gets its own line. |
| `WrapWithOverflow` | Breaks at the same break opportunities as `Wrap`, but a word that is wider than the width is not split: it stays whole on its line, and the line overflows the width, up to the next break opportunity. |

For example, with the text `"abcdefghij kl"` and a width that fits four characters, `Wrap` gives the lines `"abcd"`, `"efgh"`, `"ij"`, `"kl"`, and `WrapWithOverflow` gives `"abcdefghij"`, `"kl"`. A hyphen is a break opportunity too: `"abcdefgh-ijkl mn"` at the same width gives `"abcdefgh-"`, `"ijkl"`, `"mn"`. Text with no break opportunity at all is one line. In a `TextBlock` the measured width of an overflowing line is larger than the available width.

Wrapping is off when the available width is infinite, zero, or negative.

`TextBox.TextWrapping` uses the same enum and also defaults to `NoWrap`; a change invalidates measure and render. A `TextBox` does not pass the value through `TextAspect.Wrapping`: its text view breaks the lines itself with the same Unicode line breaker. `Wrap` breaks at word boundaries at the width of the text view, splits a word that is wider than the width, and keeps the trailing spaces of a wrapped line on that line, so `TextBox.GetLineText(0)` of `"one two three"` is `"one "` at `Width = 60`. While `Wrap` is set, the horizontal scroll bar of the text box is `Disabled`, so the text does not scroll horizontally. `WrapWithOverflow` breaks the lines of a text box at the same width but keeps a word that is wider than the text view whole on its line; only `Wrap` disables the horizontal scroll bar, so that line scrolls horizontally to the caret. `NoWrap` breaks lines only at line break characters, which a text box keeps only with `AcceptsReturn`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBox notes = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
```

Validation: `TextBlock.TextWrapping` throws `ArgumentException` for a value outside the enum, and the `TextAspect` constructor throws `ArgumentOutOfRangeException` ("Text wrapping is not supported.").

In `.crn` markup the value is the member name, matched without regard to case, for example `TextWrapping="Wrap"`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `NoWrap` | `0` | Keeps each paragraph on one line. |
| `Wrap` | `1` | Breaks lines at word boundaries, splitting words that do not fit. |
| `WrapWithOverflow` | `2` | Breaks lines at word boundaries, keeping a word that does not fit whole on its line so that the line overflows. |

## Applies to

`TextBlock.TextWrapping`, `TextBox.TextWrapping` and `TextAspect.Wrapping`.

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Controls.TextBox`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Text.LineBreakService`
