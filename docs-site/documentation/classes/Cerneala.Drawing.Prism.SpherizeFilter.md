# SpherizeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Spherize` filter.

```csharp
public sealed class SpherizeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SpherizeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Mode` | `string` | `Normal` | Optional catalog parameter; unit: `none`. |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Spherize` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
