# ExposureFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Exposure` filter.

```csharp
public sealed class ExposureFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ExposureFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Style` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `Direction` | `string` | `Forward` | Optional catalog parameter; unit: `none`. |
| `Exposure` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Gamma` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Pivot` | `float` | `0.18` | Optional catalog parameter; unit: `unitless`. |
| `LogExposureStep` | `float` | `0.088` | Optional catalog parameter; unit: `unitless`. |
| `LogMidGray` | `float` | `0.435` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Exposure` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new ExposureFilter())`.

Each property in the table also has a public static `MotionProperty<ExposureFilter, T>` field named after it, for example `StyleProperty` of type `MotionProperty<ExposureFilter, string>`. Motion uses these fields to animate the value.

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
