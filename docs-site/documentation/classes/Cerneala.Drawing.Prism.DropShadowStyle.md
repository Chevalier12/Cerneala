# DropShadowStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DropShadow` style.

```csharp
public sealed class DropShadowStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `DropShadowStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Multiply` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `UseGlobalLight` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `Angle` | `float` | `120` | Optional catalog parameter; unit: `degrees`. |
| `Distance` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Spread` | `float` | `0` | Optional catalog parameter; unit: `dip`. |
| `Size` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Contour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `LayerKnocksOut` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `DropShadow` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
