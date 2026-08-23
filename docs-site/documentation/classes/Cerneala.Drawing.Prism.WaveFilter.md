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

Parameter assignments are validated against the `Wave` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
