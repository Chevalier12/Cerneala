# ShearFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Shear` filter.

```csharp
public sealed class ShearFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ShearFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Curve` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `UndefinedAreas` | `string` | `RepeatEdgePixels` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Shear` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
