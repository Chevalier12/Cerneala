# OffsetFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Offset` filter.

```csharp
public sealed class OffsetFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `OffsetFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Offset` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |
| `UndefinedAreas` | `string` | `WrapAround` | Optional catalog parameter; unit: `none`. |
| `FillColor` | `Color` | `#00000000` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Offset` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
