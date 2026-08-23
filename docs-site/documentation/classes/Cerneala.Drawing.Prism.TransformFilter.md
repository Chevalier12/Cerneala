# TransformFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Transform` filter.

```csharp
public sealed class TransformFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TransformFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Translate` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |
| `Scale` | `Vector4` | `1, 1` | Optional catalog parameter; unit: `unitless`. |
| `Rotation` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Skew` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Origin` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Sampling` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Transparent` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Transform` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
