# SprayedStrokesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SprayedStrokes` filter.

```csharp
public sealed class SprayedStrokesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SprayedStrokesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeLength` | `float` | `12` | Optional catalog parameter; unit: `dip`. |
| `SprayRadius` | `float` | `7` | Optional catalog parameter; unit: `dip`. |
| `Direction` | `string` | `RightDiagonal` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `SprayedStrokes` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
