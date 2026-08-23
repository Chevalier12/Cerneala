# PlasticWrapFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PlasticWrap` filter.

```csharp
public sealed class PlasticWrapFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PlasticWrapFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `HighlightStrength` | `float` | `15` | Optional catalog parameter; unit: `unitless`. |
| `Detail` | `float` | `9` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `7` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `PlasticWrap` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
