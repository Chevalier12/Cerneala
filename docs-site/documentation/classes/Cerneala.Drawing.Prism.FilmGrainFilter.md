# FilmGrainFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `FilmGrain` filter.

```csharp
public sealed class FilmGrainFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `FilmGrainFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Grain` | `float` | `4` | Optional catalog parameter; unit: `unitless`. |
| `HighlightArea` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Intensity` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `FilmGrain` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
