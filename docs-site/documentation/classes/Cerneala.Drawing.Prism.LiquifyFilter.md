# LiquifyFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Liquify` filter.

```csharp
public sealed class LiquifyFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LiquifyFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Mesh` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Reconstruct` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Mask` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `MaskInvert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Liquify` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
