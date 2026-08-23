# UnderpaintingFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Underpainting` filter.

```csharp
public sealed class UnderpaintingFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `UnderpaintingFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BrushSize` | `float` | `6` | Optional catalog parameter; unit: `dip`. |
| `TextureCoverage` | `float` | `0.2` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `string` | `Canvas` | Optional catalog parameter; unit: `none`. |
| `Scaling` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Relief` | `float` | `0.04` | Optional catalog parameter; unit: `unitless`. |
| `LightDirection` | `string` | `Top` | Optional catalog parameter; unit: `none`. |
| `Invert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Underpainting` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
