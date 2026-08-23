# ReduceNoiseFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ReduceNoise` filter.

```csharp
public sealed class ReduceNoiseFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ReduceNoiseFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Strength` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `PreserveDetails` | `float` | `0.6` | Optional catalog parameter; unit: `unitless`. |
| `ReduceColorNoise` | `float` | `0.45` | Optional catalog parameter; unit: `unitless`. |
| `SharpenDetails` | `float` | `0.25` | Optional catalog parameter; unit: `unitless`. |
| `RemoveJpegArtifact` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ReduceNoise` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
