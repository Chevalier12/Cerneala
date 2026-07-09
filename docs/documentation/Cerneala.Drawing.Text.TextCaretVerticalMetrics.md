# TextCaretVerticalMetrics Struct

## Definition
Namespace: `Cerneala.Drawing.Text`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/Text/TextCaretVerticalMetrics.cs`

Stores the vertical offset and height used to draw a text caret.

```csharp
public readonly record struct TextCaretVerticalMetrics
```

Inheritance:
`Object` -> `ValueType` -> `TextCaretVerticalMetrics`

Implements:
`IEquatable<TextCaretVerticalMetrics>`

## Examples

Create caret metrics and use them to build a caret rectangle:

```csharp
using Cerneala.Drawing;
using Cerneala.Drawing.Text;

TextCaretVerticalMetrics metrics = new(offsetY: 2, height: 18);
DrawRect caretBounds = new(x: 40, y: metrics.OffsetY, width: 1, height: metrics.Height);
```

Read metrics from the text caret layout pipeline:

```csharp
using Cerneala.Drawing.Text;
using Cerneala.UI.Text;

TextAspect aspect = new("Arial", 16);
FontResolver resolver = FontResolver.Default;

TextCaretVerticalMetrics metrics =
    TextCaretLayout.Default.GetCaretVerticalMetrics(aspect, resolver);
```

## Remarks

`TextCaretVerticalMetrics` is the drawing-layer value passed from text shaping and layout code to caret rendering code. `OffsetY` is the vertical distance from the top of the text editing area to the top of the caret. `Height` is the caret height.

`TextShaper.TryMeasureCaretVerticalMetrics` creates this value from a rasterized `"Ag"` sample when the text run uses a Skia-backed font. `TextCaretLayout.GetCaretVerticalMetrics` falls back to `OffsetY` equal to `0` and `Height` equal to `TextAspect.FontSize * TextAspect.Scale` when raster metrics are unavailable.

`TextBoxBase` uses `OffsetY + Height` during measurement so the editing surface can contain the caret. During rendering, it clamps the offset and height to the available content bounds before drawing the caret.

The constructor requires a finite `offsetY` and a positive finite `height`. Because this is a struct, `default(TextCaretVerticalMetrics)` is readable and has both values equal to `0`, but it does not satisfy the constructor's positive-height invariant.

## Constructors

| Name | Description |
| --- | --- |
| `TextCaretVerticalMetrics(float offsetY, float height)` | Initializes caret vertical metrics from a finite vertical offset and a positive finite height. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `OffsetY` | `float` | Gets the vertical offset from the top of the text editing area to the top of the caret. |
| `Height` | `float` | Gets the caret height. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `TextCaretVerticalMetrics(float offsetY, float height)` | `ArgumentOutOfRangeException` | `offsetY` is `NaN`, positive infinity, or negative infinity. |
| `TextCaretVerticalMetrics(float offsetY, float height)` | `ArgumentOutOfRangeException` | `height` is less than or equal to `0`, `NaN`, positive infinity, or negative infinity. |

## Applies To

Cerneala drawing text metrics and UI text caret layout/rendering paths.

## See Also

- `Cerneala.Drawing.Text.TextShaper`
- `Cerneala.UI.Text.TextCaretLayout`
- `Cerneala.UI.Controls.TextBoxBase`
