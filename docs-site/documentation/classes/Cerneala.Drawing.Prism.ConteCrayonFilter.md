# ConteCrayonFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ConteCrayon` filter.

```csharp
public sealed class ConteCrayonFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ConteCrayonFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `ForegroundLevel` | `float` | `11` | Optional catalog parameter; unit: `unitless`. |
| `BackgroundLevel` | `float` | `7` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `string` | `Canvas` | Optional catalog parameter; unit: `none`. |
| `Scaling` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Relief` | `float` | `0.2` | Optional catalog parameter; unit: `unitless`. |
| `LightDirection` | `string` | `Top` | Optional catalog parameter; unit: `none`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ConteCrayon` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
