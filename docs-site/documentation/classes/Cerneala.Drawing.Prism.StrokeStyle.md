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

Parameter assignments are validated against the `Stroke` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`. `Vector4` is `System.Numerics.Vector4`. A `Vector4` default written with two numbers, such as `0, 0`, sets X and Y; Z and W are 0.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new StrokeStyle())`.

Each property in the table also has a public static `MotionProperty<StrokeStyle, T>` field named after it, for example `SizeProperty` of type `MotionProperty<StrokeStyle, float>`. Motion uses these fields to animate the value.

The class inherits `StyleId` and `Visible` from `PrismStyle`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static field `VisibleProperty` for the inherited `Visible`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `Vector4` property gets a NaN or infinite component; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
