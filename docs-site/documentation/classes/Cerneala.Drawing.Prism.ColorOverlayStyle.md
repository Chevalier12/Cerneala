# ColorOverlayStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColorOverlay` style.

```csharp
public sealed class ColorOverlayStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColorOverlayStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Normal` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FFFF0000` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `ColorOverlay` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
