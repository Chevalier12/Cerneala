# HighPassFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `HighPass` filter.

```csharp
public sealed class HighPassFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `HighPassFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `10` | Optional catalog parameter; unit: `dip`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `HighPass` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
