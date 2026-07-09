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
using Cerneala.UI.Text;

TextRenderer renderer = TextRenderer.Default;

TextMeasureResult result = renderer.Render(
    drawingContext,
    "Hello",
    aspect,
    availableWidth: 200,
    position: new DrawPoint(0, 0),
    color: DrawColor.Black);
```

## Remarks

`TextRenderer` uses a `TextMeasurer` to measure the supplied text before drawing. Empty text returns the measurement without drawing.

For non-empty text, `Render` resolves the font from the `TextAspect`, measures line height with `TextLineMetrics`, and draws each measured line at an offset from the supplied position. The method throws `ArgumentNullException` when `drawingContext` or `text` is `null`.

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
| `Render(DrawingContext drawingContext, string text, TextAspect aspect, float availableWidth, DrawPoint position, DrawColor color)` | `TextMeasureResult` | Measures text, draws each line when non-empty, and returns the measurement. |

## Applies To

Cerneala UI text rendering.

## See Also

- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.FontResolver`
- `Cerneala.Drawing.DrawingContext`
