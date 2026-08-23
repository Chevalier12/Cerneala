# StrokeStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Stroke` style.

```csharp
public sealed class StrokeStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `StrokeStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Size` | `float` | `3` | Optional catalog parameter; unit: `dip`. |
| `Position` | `string` | `Outside` | Optional catalog parameter; unit: `none`. |
| `BlendMode` | `string` | `Normal` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `FillType` | `string` | `Color` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Gradient` | `string` | `BlackToWhite` | Optional catalog parameter; unit: `none`. |
| `GradientMethod` | `string` | `Perceptual` | Optional catalog parameter; unit: `none`. |
| `GradientStyle` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `GradientAngle` | `float` | `90` | Optional catalog parameter; unit: `degrees`. |
| `GradientScale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `GradientAlignWithLayer` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `GradientReverse` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `GradientDither` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `GradientOffset` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |
| `Pattern` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `PatternScale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `PatternLinkWithLayer` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `PatternOffset` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `Stroke` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
