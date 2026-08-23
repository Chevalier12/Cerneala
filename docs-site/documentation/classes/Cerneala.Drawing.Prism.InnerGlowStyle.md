# InnerGlowStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `InnerGlow` style.

```csharp
public sealed class InnerGlowStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `InnerGlowStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Screen` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FFFFFFBE` | Optional catalog parameter; unit: `none`. |
| `Gradient` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Technique` | `string` | `Softer` | Optional catalog parameter; unit: `none`. |
| `Origin` | `string` | `Edge` | Optional catalog parameter; unit: `none`. |
| `Choke` | `float` | `0` | Optional catalog parameter; unit: `dip`. |
| `Size` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Contour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Range` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Jitter` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `InnerGlow` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
