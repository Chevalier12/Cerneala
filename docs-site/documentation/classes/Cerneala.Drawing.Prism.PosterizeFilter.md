# PosterizeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Posterize` filter.

```csharp
public sealed class PosterizeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PosterizeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Levels` | `float` | `4` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Posterize` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
