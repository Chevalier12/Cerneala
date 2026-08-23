# GlassFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Glass` filter.

```csharp
public sealed class GlassFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `GlassFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Distortion` | `float` | `0.2` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `string` | `Frosted` | Optional catalog parameter; unit: `none`. |
| `TextureImage` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Scaling` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Invert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Glass` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
