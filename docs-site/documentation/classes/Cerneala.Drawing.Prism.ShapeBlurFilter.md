# ShapeBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ShapeBlur` filter.

```csharp
public sealed class ShapeBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ShapeBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Shape` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Radius` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Quality` | `string` | `Good` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ShapeBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
