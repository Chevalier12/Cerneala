# MedianFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Median` filter.

```csharp
public sealed class MedianFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `MedianFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `int` | `1` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Median` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
