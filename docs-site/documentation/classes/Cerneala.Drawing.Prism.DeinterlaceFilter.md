# DeinterlaceFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Deinterlace` filter.

```csharp
public sealed class DeinterlaceFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DeinterlaceFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Field` | `string` | `Odd` | Optional catalog parameter; unit: `none`. |
| `Replacement` | `string` | `Interpolation` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Deinterlace` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
