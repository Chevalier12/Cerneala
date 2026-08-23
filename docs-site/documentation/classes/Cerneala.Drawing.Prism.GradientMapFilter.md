# GradientMapFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `GradientMap` filter.

```csharp
public sealed class GradientMapFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `GradientMapFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Gradient` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Reverse` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Dither` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Method` | `string` | `Perceptual` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `GradientMap` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
