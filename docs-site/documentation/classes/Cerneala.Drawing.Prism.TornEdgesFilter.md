# TornEdgesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `TornEdges` filter.

```csharp
public sealed class TornEdgesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TornEdgesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `ImageBalance` | `float` | `25` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `11` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `17` | Optional catalog parameter; unit: `unitless`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `TornEdges` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
