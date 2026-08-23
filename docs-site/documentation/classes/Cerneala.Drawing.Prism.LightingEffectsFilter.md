# LightingEffectsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `LightingEffects` filter.

```csharp
public sealed class LightingEffectsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LightingEffectsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Lights` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Ambient` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Metallic` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Gloss` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Exposure` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `TextureHeight` | `float` | `0` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `LightingEffects` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
