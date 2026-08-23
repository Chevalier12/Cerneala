# LensBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `LensBlur` filter.

```csharp
public sealed class LensBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LensBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `15` | Optional catalog parameter; unit: `dip`. |
| `BladeCount` | `float` | `6` | Optional catalog parameter; unit: `count`. |
| `BladeCurvature` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Rotation` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `SpecularBrightness` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `SpecularThreshold` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `DepthMap` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `DepthChannel` | `string` | `Luminance` | Optional catalog parameter; unit: `none`. |
| `FocalDistance` | `float` | `0.5` | Optional catalog parameter; unit: `dip`. |
| `InvertDepth` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `NoiseDistribution` | `string` | `Uniform` | Optional catalog parameter; unit: `none`. |
| `MonochromaticNoise` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `LensBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
