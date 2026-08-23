# SpongeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Sponge` filter.

```csharp
public sealed class SpongeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SpongeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BrushSize` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `Definition` | `float` | `12` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Sponge` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
