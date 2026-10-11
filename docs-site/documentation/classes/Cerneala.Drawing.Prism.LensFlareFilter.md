# LensFlareFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `LensFlare` filter.

```csharp
public sealed class LensFlareFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LensFlareFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Brightness` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Lens` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `LensFlare` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`. `Vector4` is `System.Numerics.Vector4`. A `Vector4` default written with two numbers, such as `0, 0`, sets X and Y; Z and W are 0.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new LensFlareFilter())`.

Each property in the table also has a public static `MotionProperty<LensFlareFilter, T>` field named after it, for example `CenterProperty` of type `MotionProperty<LensFlareFilter, Vector4>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `Vector4` property gets a NaN or infinite component. |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentOutOfRangeException` | Drawing the operation: the required `Lens` property was set to `default(PrismResourceId)`. The setter accepts that value; the first draw that uses the operation throws. A `Lens` that was never set does not throw. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
