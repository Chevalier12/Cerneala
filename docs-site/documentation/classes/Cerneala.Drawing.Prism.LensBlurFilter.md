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

Parameter assignments are validated against the `LensBlur` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new LensBlurFilter())`.

Each property in the table also has a public static `MotionProperty<LensBlurFilter, T>` field named after it, for example `RadiusProperty` of type `MotionProperty<LensBlurFilter, float>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
