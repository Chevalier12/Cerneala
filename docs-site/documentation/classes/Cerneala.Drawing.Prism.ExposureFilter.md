# ExposureFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Exposure` filter.

```csharp
public sealed class ExposureFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ExposureFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Style` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `Direction` | `string` | `Forward` | Optional catalog parameter; unit: `none`. |
| `Exposure` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Gamma` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Pivot` | `float` | `0.18` | Optional catalog parameter; unit: `unitless`. |
| `LogExposureStep` | `float` | `0.088` | Optional catalog parameter; unit: `unitless`. |
| `LogMidGray` | `float` | `0.435` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Exposure` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
