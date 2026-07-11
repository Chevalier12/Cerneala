# TextRenderer Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextRenderer.cs`

Measures and draws text lines to a drawing context.

```csharp
public class TextRenderer
```

Inheritance:
`object` -> `TextRenderer`

## Examples

Render text and receive the measurement result:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;
using Cerneala.UI.Text;

TextAspect aspect = new("Default", 16, foreground: new SolidColorBrush(Color.Black));

TextRenderer renderer = TextRenderer.Default;

TextMeasureResult result = renderer.Render(
    drawingContext,
    "Hello",
    aspect,
    availableWidth: 200,
    position: new DrawPoint(0, 0));
```

## Remarks

`TextRenderer` uses a `TextMeasurer` to measure the supplied text before drawing. Empty text returns the measurement without drawing.

For non-empty text with a non-null `TextAspect.Foreground`, `Render` resolves the font, measures line height with `TextLineMetrics`, and draws each measured line with the complete brush. Solid, gradient, image, drawing, and visual brushes are preserved in the recorded command. A brush change does not alter the text layout key. The method throws `ArgumentNullException` when `drawingContext` or `text` is `null`.

## Constructors

| Signature | Description |
| --- | --- |
| `TextRenderer()` | Initializes a renderer with default font resolution and measurement services. |
| `TextRenderer(FontResolver fontResolver, TextMeasurer textMeasurer)` | Initializes a renderer with explicit services. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Default` | `TextRenderer` | Gets the default renderer instance. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(DrawingContext drawingContext, string text, TextAspect aspect, float availableWidth, DrawPoint position)` | `TextMeasureResult` | Measures text, draws each line when text and `aspect.Foreground` are present, and returns the measurement. |

## Applies To

Cerneala UI text rendering.

## See Also

- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.FontResolver`
- `Cerneala.Drawing.DrawingContext`
