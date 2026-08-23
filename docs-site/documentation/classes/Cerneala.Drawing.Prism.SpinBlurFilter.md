# SpinBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SpinBlur` filter.

```csharp
public sealed class SpinBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SpinBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Radius` | `Vector4` | `0.25, 0.25` | Optional catalog parameter; unit: `unitless`. |
| `Rotation` | `float` | `15` | Optional catalog parameter; unit: `degrees`. |
| `Feather` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `StrobeStrength` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `StrobeFlashes` | `int` | `0` | Optional catalog parameter; unit: `count`. |
| `StrobeDuration` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `SpinBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
