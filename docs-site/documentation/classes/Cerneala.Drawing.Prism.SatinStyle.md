# SatinStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Satin` style.

```csharp
public sealed class SatinStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `SatinStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Multiply` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Angle` | `float` | `19` | Optional catalog parameter; unit: `degrees`. |
| `Distance` | `float` | `11` | Optional catalog parameter; unit: `dip`. |
| `Size` | `float` | `14` | Optional catalog parameter; unit: `dip`. |
| `Contour` | `string` | `Gaussian` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Invert` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Satin` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
