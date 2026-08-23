# MotionBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `MotionBlur` filter.

```csharp
public sealed class MotionBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `MotionBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Distance` | `float` | `10` | Optional catalog parameter; unit: `dip`. |
| `Angle` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Quality` | `string` | `Good` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Transparent` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `MotionBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
