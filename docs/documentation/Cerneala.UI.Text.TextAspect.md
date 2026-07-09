# TextAspect Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextAspect.cs`

Describes the font, sizing, wrapping, trimming, scale, color, and optional font resource used by Cerneala text services.

```csharp
public readonly record struct TextAspect
```

Inheritance:
`ValueType` -> `TextAspect`

## Examples

Create a text aspect for wrapped text and convert it to a draw text run after resolving a font:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Text;

TextAspect aspect = new(
    fontFamily: "Arial",
    fontSize: 16,
    wrapping: TextWrapping.Wrap,
    color: DrawColor.Black);

ResolvedTextFont font = FontResolver.Default.Resolve(aspect);
DrawTextRun run = aspect.ToDrawTextRun(font, "Hello");
```

## Remarks

`TextAspect` is an immutable value passed through text measurement, layout, caret, and rendering services. `TextMeasurer` uses it when building a `TextLayoutKey`, `FontResolver` uses it to resolve either `FontResourceId` or `FontFamily` plus the effective size, and `TextRenderer` uses it when drawing each measured line.

The constructor requires a non-empty font family, positive finite `fontSize`, positive finite `scale`, supported `TextWrapping` and `TextTrimming` values, and an effective text size no greater than `16384`. If `color` is omitted, `Color` is set to `DrawColor.Black`.

`ToDrawTextRun` requires a non-null `ResolvedTextFont` and non-null text. The resulting `DrawTextRun` uses the resolved draw font, the supplied text, and `FontSize * Scale` as its size.

## Constructors

| Signature | Description |
| --- | --- |
| `TextAspect(string fontFamily, float fontSize, TextWrapping wrapping = TextWrapping.NoWrap, TextTrimming trimming = TextTrimming.None, float scale = 1, DrawColor? color = null, ResourceId<FontResource>? fontResourceId = null)` | Initializes an immutable text aspect and validates font, size, scale, wrapping, trimming, and effective size values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FontFamily` | `string` | Gets the fallback or family name used when no font resource id is supplied. |
| `FontSize` | `float` | Gets the base font size before `Scale` is applied. |
| `Color` | `DrawColor` | Gets the text color stored in the aspect. Defaults to `DrawColor.Black`. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets the optional font resource id resolved by `FontResolver` before falling back to `FontFamily`. |
| `Wrapping` | `TextWrapping` | Gets the wrapping mode used by measurement and line breaking. |
| `Trimming` | `TextTrimming` | Gets the trimming mode included in layout cache keys. |
| `Scale` | `float` | Gets the text scale applied to font size for font resolution and draw text runs. |

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
