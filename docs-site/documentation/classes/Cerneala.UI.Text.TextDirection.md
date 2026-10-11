# TextDirection Enum

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextDirection.cs`

Specifies the reading direction of a piece of text.

```csharp
public enum TextDirection
```

## Examples

```csharp
using Cerneala.UI.Text;

BidiTextService bidi = BidiTextService.Default;
Console.WriteLine(bidi.GetBaseDirection("Hello"));
Console.WriteLine(bidi.GetBaseDirection("שלום abc"));
Console.WriteLine(string.Join(", ", bidi.GetDirectionalRuns("abc שלום")));
// LeftToRight
// RightToLeft
// BidiTextRun { Start = 0, Length = 4, Direction = LeftToRight }, BidiTextRun { Start = 4, Length = 4, Direction = RightToLeft }
```

## Remarks

`BidiTextService` produces these values. `GetBaseDirection` returns the direction of the first strong character, or `LeftToRight` when the text has none, for example `""` or `"123 !"`. `GetDirectionalRuns` gives each run `LeftToRight` or `RightToLeft`; neutral characters such as spaces join a neighboring run. So the service does not return `Neutral` in practice.

No Cerneala control reads this enum. `TextBlock`, `TextAspect`, and `LineBreakService` take no direction input, and there is no `FlowDirection` property.

`Cerneala.Drawing.DrawTextDirection` is a separate enum (`Auto`, `LeftToRight`, `RightToLeft`) used by `DrawTextLayoutOptions`. The two enums are not converted to each other.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Neutral` | `0` | No strong direction. |
| `LeftToRight` | `1` | Left-to-right text, such as Latin script. |
| `RightToLeft` | `2` | Right-to-left text, such as Hebrew or Arabic script. |

## Applies to

`BidiTextService.GetBaseDirection(string)` and `BidiTextRun.Direction`.

## See also

- `Cerneala.UI.Text.BidiTextService`
- `Cerneala.UI.Text.BidiTextRun`
- `Cerneala.Drawing.DrawTextDirection`
