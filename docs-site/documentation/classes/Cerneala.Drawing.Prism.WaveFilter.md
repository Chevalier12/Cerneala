# WaveFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Wave` filter.

```csharp
public sealed class WaveFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `WaveFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Generators` | `float` | `5` | Optional catalog parameter; unit: `count`. |
| `Wavelength` | `Vector4` | `10, 120` | Optional catalog parameter; unit: `unitless`. |
| `Amplitude` | `Vector4` | `5, 35` | Optional catalog parameter; unit: `unitless`. |
| `Scale` | `Vector4` | `1, 1` | Optional catalog parameter; unit: `unitless`. |
| `Type` | `string` | `Sine` | Optional catalog parameter; unit: `none`. |
| `UndefinedAreas` | `string` | `RepeatEdgePixels` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Wave` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`Vector4` is `System.Numerics.Vector4`. A `Vector4` default written with two numbers, such as `0, 0`, sets X and Y; Z and W are 0.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new WaveFilter())`.

Each property in the table also has a public static `MotionProperty<WaveFilter, T>` field named after it, for example `GeneratorsProperty` of type `MotionProperty<WaveFilter, float>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; an `int` property is set outside the catalog `minimum`/`maximum`; a `Vector4` property gets a NaN or infinite component; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
