# TraceContourFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `TraceContour` filter.

```csharp
public sealed class TraceContourFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TraceContourFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Level` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Edge` | `string` | `Lower` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `TraceContour` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
