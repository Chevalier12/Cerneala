# ReticulationFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Reticulation` filter.

```csharp
public sealed class ReticulationFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ReticulationFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Density` | `float` | `12` | Optional catalog parameter; unit: `unitless`. |
| `ForegroundLevel` | `float` | `40` | Optional catalog parameter; unit: `unitless`. |
| `BackgroundLevel` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Reticulation` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
