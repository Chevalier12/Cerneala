# TextMeasureResult Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextMeasureResult.cs`

Stores the result of measuring text layout.

```csharp
public sealed class TextMeasureResult
```

Inheritance:
`object` -> `TextMeasureResult`

## Examples

Inspect a measurement result:

```csharp
using Cerneala.UI.Text;

TextMeasureResult result = MeasureText();

float width = result.Size.Width;
int lineCount = result.LineCount;
IReadOnlyList<TextLine> lines = result.Lines;
```

## Remarks

`TextMeasureResult` stores the measured size, line count, cache key, resolved font identity, and measured text lines.

The constructor clamps `Size` to non-negative values. It throws `ArgumentOutOfRangeException` when `lineCount` is negative, throws `ArgumentException` when `resolvedFontIdentity` is empty or whitespace, and throws `ArgumentNullException` when `lines` is `null`.

## Constructors

| Signature | Description |
| --- | --- |
| `TextMeasureResult(LayoutSize size, int lineCount, TextLayoutKey cacheKey, string resolvedFontIdentity, IReadOnlyList<TextLine> lines)` | Initializes a text measurement result. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Size` | `LayoutSize` | Gets the measured size clamped to non-negative dimensions. |
| `LineCount` | `int` | Gets the measured line count. |
| `CacheKey` | `TextLayoutKey` | Gets the cache key used for this measurement. |
| `ResolvedFontIdentity` | `string` | Gets the identity of the resolved font used for measurement. |
| `Lines` | `IReadOnlyList<TextLine>` | Gets the measured lines. |

## Applies To

Cerneala UI text measurement and layout caching.

## See Also

- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.TextLayoutCache`
- `Cerneala.UI.Text.TextLine`
- `Cerneala.UI.Layout.LayoutSize`
