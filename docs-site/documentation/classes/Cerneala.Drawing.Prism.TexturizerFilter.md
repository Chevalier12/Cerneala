# TexturizerFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Texturizer` filter.

```csharp
public sealed class TexturizerFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TexturizerFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Texture` | `string` | `Canvas` | Optional catalog parameter; unit: `none`. |
| `TextureImage` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Scaling` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Relief` | `float` | `0.04` | Optional catalog parameter; unit: `unitless`. |
| `LightDirection` | `string` | `Top` | Optional catalog parameter; unit: `none`. |
| `Invert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Texturizer` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
