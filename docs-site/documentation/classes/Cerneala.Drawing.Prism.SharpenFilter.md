# SharpenFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Sharpen` filter.

```csharp
public sealed class SharpenFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SharpenFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `0.25` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Sharpen` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
