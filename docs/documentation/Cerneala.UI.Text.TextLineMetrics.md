# TextLineMetrics Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextLineMetrics.cs`

Provides shared line-height measurement for UI text layout and rendering.

```csharp
internal static class TextLineMetrics
```

Inheritance:
`object` -> `TextLineMetrics`

## Remarks

`TextLineMetrics` is an internal helper used by `TextMeasurer` and `TextRenderer` so measured text height and rendered line spacing use the same line-height calculation.

`MeasureLineHeight` builds a `DrawTextRun` from the supplied `TextAspect`, resolved font, and the sample text `"Ag"`, then asks `TextShaper.Default` for a rasterized line height. When the current drawing font cannot provide shaper metrics, the method falls back to the effective text size, `TextAspect.FontSize * TextAspect.Scale`.

The supplied `font` cannot be `null`. `TextAspect.ToDrawTextRun` also validates the resolved font before creating the draw run.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `MeasureLineHeight(TextAspect aspect, ResolvedTextFont font)` | `float` | Returns the measured line height for the text aspect and resolved font, or the effective text size when shaper metrics are unavailable. |

## Method Details

### MeasureLineHeight

```csharp
public static float MeasureLineHeight(TextAspect aspect, ResolvedTextFont font)
```

#### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `aspect` | `TextAspect` | Font size, scale, and font conversion settings used to create the draw text run. |
| `font` | `ResolvedTextFont` | The resolved drawing font used for shaper measurement. Cannot be `null`. |

#### Returns

`float`

The rasterized height of the `"Ag"` sample when `TextShaper.Default.TryMeasureLineHeight` succeeds; otherwise `aspect.FontSize * aspect.Scale`.

#### Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `font` is `null`. |

## Applies To

Cerneala UI text measurement and rendering internals.

## See Also

- `Cerneala.UI.Text.TextMeasurer`
- `Cerneala.UI.Text.TextRenderer`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Text.ResolvedTextFont`
- `Cerneala.Drawing.Text.TextShaper`
