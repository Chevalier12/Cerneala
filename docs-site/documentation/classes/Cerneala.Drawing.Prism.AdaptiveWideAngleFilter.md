# AdaptiveWideAngleFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `AdaptiveWideAngle` filter.

```csharp
public sealed class AdaptiveWideAngleFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `AdaptiveWideAngleFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `FocalLength` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `normalized-coordinate`. |
| `PrincipalPoint` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `normalized-coordinate`. |
| `DistortionCoefficients` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `AdaptiveWideAngle` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
