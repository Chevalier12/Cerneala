# AverageFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Average` filter.

```csharp
public sealed class AverageFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `AverageFilter()` | Creates the operation with Prism catalog defaults. |

## Remarks

Parameter assignments are validated against the `Average` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
