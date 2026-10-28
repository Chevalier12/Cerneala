# TransformFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Transform` filter.

```csharp
public sealed class TransformFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TransformFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Translate` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |
| `Scale` | `Vector4` | `1, 1` | Optional catalog parameter; unit: `unitless`. |
| `Rotation` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Skew` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Origin` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Sampling` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Transparent` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Transform` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`Vector4` is `System.Numerics.Vector4`. A `Vector4` default written with two numbers, such as `0, 0`, sets X and Y; Z and W are 0.

The transform is rigid in device pixels on the CPU reference and the SDL_GPU kernel: the filter scales, skews, and rotates the layer about `Origin` (a fraction of the layer bounds), then translates it, and the result stays inside the bounds that the graph optimizer computes for the same transform. A non-square layer is not sheared. For example, `Rotation = 90` on an opaque 4x2 px layer with `EdgeMode = "Transparent"` gives a 2x4 px block about the center: columns 1 and 2 stay opaque, and columns 0 and 3 become transparent.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new TransformFilter())`.

Each property in the table also has a public static `MotionProperty<TransformFilter, T>` field named after it, for example `TranslateProperty` of type `MotionProperty<TransformFilter, Vector4>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `Vector4` property gets a NaN or infinite component; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
