# FragmentFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Fragment` filter.

```csharp
public sealed class FragmentFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `FragmentFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Offset` | `float` | `1` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `Fragment` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
