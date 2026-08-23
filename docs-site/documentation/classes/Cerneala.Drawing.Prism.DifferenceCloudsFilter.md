# DifferenceCloudsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DifferenceClouds` filter.

```csharp
public sealed class DifferenceCloudsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DifferenceCloudsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |
| `Scale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |
| `DirectionCount` | `int` | `20` | Optional catalog parameter; unit: `count`. |
| `SliceThickness` | `float` | `4` | Optional catalog parameter; unit: `unitless`. |
| `FrequencyRange` | `Vector4` | `0.03125, 1` | Optional catalog parameter; unit: `unitless`. |
| `Anisotropy` | `Vector4` | `0, 1` | Optional catalog parameter; unit: `unitless`. |
| `Spectrum` | `string` | `Brown` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `DifferenceClouds` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
