# InvertFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Invert` filter.

```csharp
public sealed class InvertFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `InvertFilter()` | Creates the operation with Prism catalog defaults. |

## Remarks

Parameter assignments are validated against the `Invert` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
