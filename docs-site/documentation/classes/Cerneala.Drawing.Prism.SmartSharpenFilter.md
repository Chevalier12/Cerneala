# SmartSharpenFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SmartSharpen` filter.

```csharp
public sealed class SmartSharpenFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SmartSharpenFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Radius` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `ReduceNoise` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |
| `Remove` | `string` | `GaussianBlur` | Optional catalog parameter; unit: `none`. |
| `Angle` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `ShadowFade` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `ShadowTonalWidth` | `float` | `0.5` | Optional catalog parameter; unit: `dip`. |
| `ShadowRadius` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `HighlightFade` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `HighlightTonalWidth` | `float` | `0.5` | Optional catalog parameter; unit: `dip`. |
| `HighlightRadius` | `float` | `1` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `SmartSharpen` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
