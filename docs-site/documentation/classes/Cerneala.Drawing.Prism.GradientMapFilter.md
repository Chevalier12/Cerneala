# GradientMapFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `GradientMap` filter.

```csharp
public sealed class GradientMapFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `GradientMapFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Gradient` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Reverse` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Dither` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Method` | `string` | `Perceptual` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `GradientMap` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new GradientMapFilter())`.

Each property in the table also has a public static `MotionProperty<GradientMapFilter, T>` field named after it, for example `GradientProperty` of type `MotionProperty<GradientMapFilter, PrismResourceId>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |
| `ArgumentOutOfRangeException` | Drawing the operation: the required `Gradient` property was set to `default(PrismResourceId)`. The setter accepts that value; the first draw that uses the operation throws. A `Gradient` that was never set does not throw. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
