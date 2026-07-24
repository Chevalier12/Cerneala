# TextCaretLayout Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextCaretLayout.cs`

Calculates caret positions, hit-test indexes, and vertical caret metrics for single-line text editing.

```csharp
public sealed class TextCaretLayout
```

Inheritance:
`object` -> `TextCaretLayout`

## Examples

Measure a caret stop and map a pointer x-coordinate back to a text index:

```csharp
using Cerneala.Drawing.Text;
using Cerneala.UI.Text;

TextCaretLayout layout = TextCaretLayout.Default;
TextAspect aspect = new("Arial", 20);
FontResolver resolver = new(new SystemFontSource());

string text = "iiiiWWWW";
float caretX = layout.GetCaretX(text, position: 4, aspect, resolver);
int index = layout.GetCaretIndexAtX(text, caretX + 2, aspect, resolver);
```

## Remarks

`TextCaretLayout` is used by text editing controls to place the insertion caret, select text from pointer coordinates, keep the caret visible in a horizontally scrolled text viewport, and size the rendered caret vertically.

Horizontal caret positions are based on shaped prefix advance widths when the configured font can be shaped. If shaping is not available, measurement falls back to `TextMeasurer.Default`. `GetCaretX` clamps requested positions into the text range and normalizes them to text-element boundaries so callers do not place the caret in the middle of a combining character sequence.

Hit testing builds the same text-element caret stops and returns the nearest valid caret index. The overload that accepts `horizontalTextOffset` maps viewport coordinates into text coordinates before choosing the nearest stop.

Vertical metrics are measured from the shaped/rasterized `"Ag"` sample when available. If font metrics are unavailable, line height and caret height fall back to `TextAspect.FontSize * TextAspect.Scale` with an offset of `0`.

`text` and `resolver` parameters cannot be `null`. Font resolution can throw if the supplied `TextAspect` requires a font resource and the `FontResolver` has no resource provider.

## Constructors

| Name | Description |
| --- | --- |
| `TextCaretLayout()` | Creates a caret layout service. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `TextCaretLayout` | Gets the shared default caret layout service. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetCaretX(string text, int position, TextAspect aspect, FontResolver resolver)` | `float` | Returns the x-coordinate of the caret after clamping and normalizing `position` to a valid text-element boundary. |
| `GetCaretIndexAtX(string text, float x, TextAspect aspect, FontResolver resolver)` | `int` | Returns the nearest valid caret index for a text-coordinate x-position. |
| `GetCaretIndexAtX(string text, float x, float horizontalTextOffset, TextAspect aspect, FontResolver resolver)` | `int` | Returns the nearest valid caret index after applying a horizontal viewport text offset. |
| `GetCaretLineHeight(TextAspect aspect, FontResolver resolver)` | `float` | Returns the measured caret line height, or the effective text size when raster metrics are unavailable. |
| `GetCaretVerticalMetrics(TextAspect aspect, FontResolver resolver)` | `TextCaretVerticalMetrics` | Returns the vertical offset and height used to draw the caret. |

## Method Details

### GetCaretX

```csharp
public float GetCaretX(string text, int position, TextAspect aspect, FontResolver resolver)
```

Returns `0` at the beginning of the text. Positions before the start clamp to `0`; positions past the end clamp to `text.Length`. Positions inside a text element normalize to the previous text-element boundary.

### GetCaretIndexAtX

```csharp
public int GetCaretIndexAtX(string text, float x, TextAspect aspect, FontResolver resolver)
public int GetCaretIndexAtX(string text, float x, float horizontalTextOffset, TextAspect aspect, FontResolver resolver)
```

Returns `0` for coordinates before the first caret stop and `text.Length` for coordinates after the final stop. Empty text always returns `0`.

### GetCaretLineHeight

```csharp
public float GetCaretLineHeight(TextAspect aspect, FontResolver resolver)
```

Uses font raster metrics when they are available; otherwise returns the effective text size.

### GetCaretVerticalMetrics

```csharp
public TextCaretVerticalMetrics GetCaretVerticalMetrics(TextAspect aspect, FontResolver resolver)
```

Uses font raster metrics when they are available; otherwise returns `new TextCaretVerticalMetrics(0, aspect.FontSize * aspect.Scale)`.

## Applies To

Cerneala UI text editing and single-line text box caret layout.

## See Also

- `Cerneala.UI.Controls.TextBox`
- `Cerneala.UI.Controls.PasswordBox`
- `Cerneala.UI.Text.TextCaret`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Text.FontResolver`
- `Cerneala.Drawing.Text.TextCaretVerticalMetrics`
