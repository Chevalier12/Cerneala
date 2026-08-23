# InkOutlinesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `InkOutlines` filter.

```csharp
public sealed class InkOutlinesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `InkOutlinesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeLength` | `float` | `4` | Optional catalog parameter; unit: `dip`. |
| `DarkIntensity` | `float` | `20` | Optional catalog parameter; unit: `unitless`. |
| `LightIntensity` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `InkOutlines` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
