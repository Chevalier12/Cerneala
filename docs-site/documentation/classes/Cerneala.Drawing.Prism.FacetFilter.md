# FacetFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Facet` filter.

```csharp
public sealed class FacetFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `FacetFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Iterations` | `float` | `1` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Facet` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
