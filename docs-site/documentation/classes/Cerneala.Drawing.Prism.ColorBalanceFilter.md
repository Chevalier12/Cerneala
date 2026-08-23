# ColorBalanceFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColorBalance` filter.

```csharp
public sealed class ColorBalanceFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColorBalanceFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Shadows` | `Vector4` | `0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Midtones` | `Vector4` | `0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Highlights` | `Vector4` | `0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `PreserveLuminosity` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ColorBalance` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
