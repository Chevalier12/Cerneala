# ColorMatrixFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColorMatrix` filter.

```csharp
public sealed class ColorMatrixFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColorMatrixFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Matrix` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Clamp` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ColorMatrix` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
