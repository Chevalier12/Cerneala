# BlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Blur` filter.

```csharp
public sealed class BlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `BlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `Quality` | `string` | `Good` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Blur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
