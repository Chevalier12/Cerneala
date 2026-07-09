# BidiTextService Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/BidiTextService.cs`

Provides basic bidirectional text classification helpers for finding base text direction, directional runs, and right-to-left content.

```csharp
public sealed class BidiTextService
```

Inheritance:
`Object` -> `BidiTextService`

## Examples
The following example gets the base direction and directional runs for mixed left-to-right and Hebrew text.

```csharp
using Cerneala.UI.Text;

BidiTextService service = BidiTextService.Default;

TextDirection baseDirection = service.GetBaseDirection("  hello");
IReadOnlyList<BidiTextRun> runs = service.GetDirectionalRuns("abc \u05E9\u05DC\u05D5\u05DD");
bool hasRightToLeft = service.ContainsRightToLeft("abc \u05E9\u05DC\u05D5\u05DD");
```

## Remarks
`BidiTextService` classifies each character by Unicode category and by a set of right-to-left Unicode ranges. It returns `TextDirection.RightToLeft` for right-to-left letters in the Hebrew/Arabic blocks and presentation forms, `TextDirection.LeftToRight` for letters and decimal digits that are not classified as right-to-left, and `TextDirection.Neutral` for other characters during internal classification.

`GetBaseDirection` skips neutral characters and returns the first strong left-to-right or right-to-left direction found in the input. If the input is `null`, empty, or contains only neutral characters, it returns `TextDirection.LeftToRight`.

`GetDirectionalRuns` groups adjacent characters into `BidiTextRun` values. Neutral characters are normalized to the current run direction; the first character uses `TextDirection.LeftToRight` as the fallback direction. Passing `null` is treated the same as passing an empty string.

The service is stateless. Use `Default` when a shared instance is enough, or create a new instance when a caller wants its own service object.

## Constructors
| Name | Description |
| --- | --- |
| `BidiTextService()` | Initializes a new instance of the `BidiTextService` class. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Default` | `BidiTextService` | Gets a shared default service instance. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `GetBaseDirection(string text)` | `TextDirection` | Returns the first strong direction in `text`, or `TextDirection.LeftToRight` when no strong direction is found. |
| `GetDirectionalRuns(string text)` | `IReadOnlyList<BidiTextRun>` | Returns directional runs for `text`, grouping adjacent characters that share the normalized direction. |
| `ContainsRightToLeft(string text)` | `bool` | Returns `true` when `GetDirectionalRuns` contains at least one `TextDirection.RightToLeft` run. |

## Related Supporting Types
| Name | Description |
| --- | --- |
| `BidiTextRun` | Immutable record struct declared in the same source file with `Start`, `Length`, and `Direction` values for a directional run. |
| `TextDirection` | Enum declared in the same source file with `Neutral`, `LeftToRight`, and `RightToLeft` values. |

## Applies to
Project: `Cerneala`

## See also
- `BidiTextRun`
- `TextDirection`
