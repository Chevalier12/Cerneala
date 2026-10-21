# TextAspect Struct

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextAspect.cs`

Describes the font, sizing, wrapping, trimming, scale, foreground brush, and optional font resource used by Cerneala text services.

```csharp
public readonly record struct TextAspect
```

Inheritance:
`ValueType` -> `TextAspect`

## Examples

Create a text aspect for wrapped text and convert it to a draw text run after resolving a font:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

TextAspect aspect = new(
    fontFamily: "Arial",
    fontSize: 16,
    wrapping: TextWrapping.Wrap,
    foreground: new SolidColorBrush(Color.Black));

ResolvedTextFont font = FontResolver.Default.Resolve(aspect);
DrawTextRun run = aspect.ToDrawTextRun(font, "Hello");
```

## Remarks

`TextAspect` is an immutable value passed through text measurement, layout, caret, and rendering services. `TextMeasurer` uses it when building a `TextLayoutKey`, `FontResolver` uses it to resolve either `FontResourceId` or `FontFamily` plus the effective size, and `TextRenderer` uses it when drawing each measured line.

The constructor requires a non-empty font family, positive finite `fontSize` and `scale` that are each no greater than `16384`, supported `TextWrapping` and `TextTrimming` values, and an effective text size no greater than `16384`. `Foreground` may be `null`; measurement and shaping remain independent of the brush, while `TextRenderer` skips drawing when no foreground is supplied.

`FontWeight`, `FontStyle`, and `FontStretch` select the face of `FontFamily`. They are part of the identity of the resolved font, so aspects that differ in them never share a cached layout, while an aspect with all three `Normal` keeps the identity it had before they existed. Example: `new TextAspect("Arial", 16, fontWeight: FontWeight.Bold)` resolves to the identity `"Arial:16:700:Normal:Normal"`.

`ToDrawTextRun` requires a non-null `ResolvedTextFont` and non-null text. The resulting `DrawTextRun` uses the resolved draw font, the supplied text, and `FontSize * Scale` as its size.

## Constructors

| Signature | Description |
| --- | --- |
| `TextAspect(string fontFamily, float fontSize, TextWrapping wrapping = TextWrapping.NoWrap, TextTrimming trimming = TextTrimming.None, float scale = 1, Brush? foreground = null, ResourceId<FontResource>? fontResourceId = null, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal)` | Initializes an immutable text aspect and validates font, size, scale, wrapping, trimming, effective size, weight, style, and stretch values. Throws `ArgumentOutOfRangeException` for a `fontWeight`, `fontStyle`, or `fontStretch` that is not a defined enum member. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FontFamily` | `string` | Gets the fallback or family name used when no font resource id is supplied. |
| `FontSize` | `float` | Gets the base font size before `Scale` is applied. |
| `Foreground` | `Brush?` | Gets the brush used by `TextRenderer`. The brush is excluded from text layout cache keys. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets the optional font resource id resolved by `FontResolver` before falling back to `FontFamily`. |
| `Wrapping` | `TextWrapping` | Gets the wrapping mode used by measurement and line breaking. |
| `Trimming` | `TextTrimming` | Gets the trimming mode included in layout cache keys. |
| `Scale` | `float` | Gets the text scale applied to font size for font resolution and draw text runs. |
| `FontWeight` | `FontWeight` | Gets the weight of the font face. The default is `FontWeight.Normal`. Ignored when `FontResourceId` is set. |
| `FontStyle` | `FontStyle` | Gets the slant of the font face. The default is `FontStyle.Normal`. Ignored when `FontResourceId` is set. |
| `FontStretch` | `FontStretch` | Gets the width of the font face. The default is `FontStretch.Normal`. Ignored when `FontResourceId` is set. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToDrawTextRun(ResolvedTextFont font, string text)` | `DrawTextRun` | Creates a draw text run from a resolved font, text, and the effective size `FontSize * Scale`. |

## Applies To

Cerneala UI text measurement, layout, caret positioning, and rendering.

## See Also

- `Cerneala.UI.Text.FontResolver`
- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.TextRenderer`
- `Cerneala.UI.Text.TextLayoutKey`
