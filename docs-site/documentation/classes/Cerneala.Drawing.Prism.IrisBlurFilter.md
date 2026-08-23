# IrisBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `IrisBlur` filter.

```csharp
public sealed class IrisBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `IrisBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Radius` | `Vector4` | `0.25, 0.25` | Optional catalog parameter; unit: `dip`. |
| `Feather` | `float` | `0.5` | Optional catalog parameter; unit: `dip`. |
| `Rotation` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Blur` | `float` | `15` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `IrisBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
