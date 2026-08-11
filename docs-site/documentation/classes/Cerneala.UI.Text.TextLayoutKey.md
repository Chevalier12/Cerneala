# TextLayoutKey Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextLayoutKey.cs`

Identifies a cached text layout measurement.

```csharp
public readonly record struct TextLayoutKey(
    string Text,
    string FontIdentity,
    float FontSize,
    TextWrapping Wrapping,
    float WrappingWidth,
    TextTrimming Trimming,
    float Scale,
    int VisibleLineCount = int.MaxValue)
```

Inheritance:
`ValueType` -> `TextLayoutKey`

## Examples

Use a layout key with `TextLayoutCache`:

```csharp
using Cerneala.UI.Text;

TextLayoutKey key = new(
    Text: "Hello",
    FontIdentity: "Arial:16",
    FontSize: 16,
    Wrapping: TextWrapping.NoWrap,
    WrappingWidth: float.PositiveInfinity,
    Trimming: TextTrimming.None,
    Scale: 1);

bool cached = cache.Contains(key);
```

## Remarks

`TextLayoutKey` is an immutable value used as the dictionary key for text layout caching. It combines the text content, resolved font identity, font size, wrapping mode and width, trimming mode, scale, and effective visible-line count. `VisibleLineCount` remains `int.MaxValue` for unconstrained or fully visible layouts and contains the collapsed line count for height-limited ellipsis layouts.

The type does not validate constructor values. It relies on callers to supply values that match the measurement request.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the text being measured. |
| `FontIdentity` | `string` | Gets the identity of the resolved font. |
| `FontSize` | `float` | Gets the requested font size. |
| `Wrapping` | `TextWrapping` | Gets the wrapping mode. |
| `WrappingWidth` | `float` | Gets the available wrapping width. |
| `Trimming` | `TextTrimming` | Gets the trimming mode. |
| `Scale` | `float` | Gets the text scale. |
| `VisibleLineCount` | `int` | Gets the effective visible-line limit used by height-aware trimming. Defaults to `int.MaxValue`. |

## Applies To

Cerneala UI text layout caching.

## See Also

- `Cerneala.UI.Text.TextLayoutCache`
- `Cerneala.UI.Text.TextMeasurer`
