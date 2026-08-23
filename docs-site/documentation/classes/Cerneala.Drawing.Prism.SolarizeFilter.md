# SolarizeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Solarize` filter.

```csharp
public sealed class SolarizeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SolarizeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Threshold` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Solarize` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
