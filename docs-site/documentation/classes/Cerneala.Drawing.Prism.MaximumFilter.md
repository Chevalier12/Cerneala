# MaximumFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Maximum` filter.

```csharp
public sealed class MaximumFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `MaximumFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `Preserve` | `string` | `Roundness` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Maximum` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
