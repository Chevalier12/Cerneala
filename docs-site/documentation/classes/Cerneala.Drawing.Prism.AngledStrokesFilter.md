# AngledStrokesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `AngledStrokes` filter.

```csharp
public sealed class AngledStrokesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `AngledStrokesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `DirectionBalance` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `StrokeLength` | `float` | `15` | Optional catalog parameter; unit: `dip`. |
| `Sharpness` | `float` | `3` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `AngledStrokes` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
