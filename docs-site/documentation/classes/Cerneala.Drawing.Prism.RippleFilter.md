# RippleFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Ripple` filter.

```csharp
public sealed class RippleFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `RippleFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Size` | `string` | `Medium` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |
| `EdgeMode` | `string` | `Repeat` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Ripple` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
