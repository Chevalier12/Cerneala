# LensCorrectionFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `LensCorrection` filter.

```csharp
public sealed class LensCorrectionFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LensCorrectionFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Distortion` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `ChromaticRedCyan` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `ChromaticBlueYellow` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `VignetteAmount` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `VignetteMidpoint` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `PerspectiveVertical` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `PerspectiveHorizontal` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Angle` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Scale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `EdgeMode` | `string` | `Transparent` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `LensCorrection` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
