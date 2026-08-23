# StainedGlassFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `StainedGlass` filter.

```csharp
public sealed class StainedGlassFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `StainedGlassFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CellSize` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `BorderThickness` | `float` | `4` | Optional catalog parameter; unit: `dip`. |
| `LightIntensity` | `float` | `3` | Optional catalog parameter; unit: `unitless`. |
| `BorderColor` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `StainedGlass` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
