# VibranceFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Vibrance` filter.

```csharp
public sealed class VibranceFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `VibranceFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Saturation` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `AvoidSaturatingSkinTones` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `GrayColorTransform` | `Vector4` | `0.2126, 0.7152, 0.0722` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Vibrance` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
