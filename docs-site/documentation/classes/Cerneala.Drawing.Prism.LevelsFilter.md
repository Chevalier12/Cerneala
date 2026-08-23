# LevelsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Levels` filter.

```csharp
public sealed class LevelsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LevelsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Channel` | `string` | `Composite` | Optional catalog parameter; unit: `none`. |
| `Auto` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `InputBlack` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `InputWhite` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Gamma` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `OutputBlack` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `OutputWhite` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Levels` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
