# WatercolorFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Watercolor` filter.

```csharp
public sealed class WatercolorFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `WatercolorFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BrushDetail` | `float` | `9` | Optional catalog parameter; unit: `unitless`. |
| `ShadowIntensity` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `float` | `3` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Watercolor` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
