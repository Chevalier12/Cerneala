# OilPaintFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `OilPaint` filter.

```csharp
public sealed class OilPaintFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `OilPaintFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Stylization` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Cleanliness` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Scale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `BristleDetail` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Lighting` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `Angle` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Shine` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `OilPaint` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
