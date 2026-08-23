# TiltShiftFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `TiltShift` filter.

```csharp
public sealed class TiltShiftFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TiltShiftFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Angle` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `FocusWidth` | `float` | `0.25` | Optional catalog parameter; unit: `dip`. |
| `Feather` | `float` | `0.25` | Optional catalog parameter; unit: `dip`. |
| `Blur` | `float` | `15` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `TiltShift` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
