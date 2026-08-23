# DiffuseGlowFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DiffuseGlow` filter.

```csharp
public sealed class DiffuseGlowFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DiffuseGlowFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Grain` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `GlowAmount` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |
| `ClearAmount` | `float` | `0.15` | Optional catalog parameter; unit: `unitless`. |
| `Color` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `DiffuseGlow` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
