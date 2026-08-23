# SpatterFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Spatter` filter.

```csharp
public sealed class SpatterFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SpatterFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `SprayRadius` | `float` | `10` | Optional catalog parameter; unit: `dip`. |
| `Smoothness` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Spatter` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
