# OceanRippleFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `OceanRipple` filter.

```csharp
public sealed class OceanRippleFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `OceanRippleFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `RippleSize` | `float` | `0.5` | Optional catalog parameter; unit: `dip`. |
| `RippleMagnitude` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `OceanRipple` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
