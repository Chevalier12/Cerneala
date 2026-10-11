# GrainFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Grain` filter.

```csharp
public sealed class GrainFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `GrainFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Intensity` | `float` | `40` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `50` | Optional catalog parameter; unit: `unitless`. |
| `Type` | `string` | `Regular` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Grain` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new GrainFilter())`.

Each property in the table also has a public static `MotionProperty<GrainFilter, T>` field named after it, for example `IntensityProperty` of type `MotionProperty<GrainFilter, float>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; an `int` property is set outside the catalog `minimum`/`maximum`; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
