# PatternOverlayStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PatternOverlay` style.

```csharp
public sealed class PatternOverlayStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `PatternOverlayStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Normal` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Pattern` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Scale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `LinkWithLayer` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `Offset` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `PatternOverlay` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
