# EmbossFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Emboss` filter.

```csharp
public sealed class EmbossFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `EmbossFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Angle` | `float` | `135` | Optional catalog parameter; unit: `degrees`. |
| `Height` | `float` | `3` | Optional catalog parameter; unit: `dip`. |
| `Amount` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Emboss` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
