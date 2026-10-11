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

Wrapping is off when the available width is infinite, zero, or negative.

Validation: `TextBlock.TextWrapping` throws `ArgumentException` for a value outside the enum, and the `TextAspect` constructor throws `ArgumentOutOfRangeException` ("Text wrapping is not supported.").

In `.crn` markup the value is the member name, matched without regard to case, for example `TextWrapping="Wrap"`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `NoWrap` | `0` | Keeps each paragraph on one line. |
| `Wrap` | `1` | Breaks lines at word boundaries, splitting words that do not fit. |

## Applies to

`TextBlock.TextWrapping` and `TextAspect.Wrapping`.

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Text.LineBreakService`
