# GrainFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Grain` filter.

```csharp
public sealed class GrainFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `GrainFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Intensity` | `float` | `40` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `50` | Optional catalog parameter; unit: `unitless`. |
| `Type` | `string` | `Regular` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Grain` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
