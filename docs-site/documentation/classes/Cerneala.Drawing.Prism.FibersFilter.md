# FibersFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Fibers` filter.

```csharp
public sealed class FibersFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `FibersFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |
| `Variance` | `float` | `16` | Optional catalog parameter; unit: `unitless`. |
| `Strength` | `float` | `4` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Fibers` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
