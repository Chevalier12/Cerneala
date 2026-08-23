# AddNoiseFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `AddNoise` filter.

```csharp
public sealed class AddNoiseFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `AddNoiseFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |
| `Distribution` | `string` | `Uniform` | Optional catalog parameter; unit: `none`. |
| `Monochromatic` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `AddNoise` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
