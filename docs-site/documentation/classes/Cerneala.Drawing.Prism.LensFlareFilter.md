# LensFlareFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `LensFlare` filter.

```csharp
public sealed class LensFlareFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LensFlareFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Brightness` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Lens` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `LensFlare` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
