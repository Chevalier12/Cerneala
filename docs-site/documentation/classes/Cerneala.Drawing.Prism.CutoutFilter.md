# CutoutFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Cutout` filter.

```csharp
public sealed class CutoutFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `CutoutFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Levels` | `float` | `8` | Optional catalog parameter; unit: `count`. |
| `EdgeSimplicity` | `float` | `4` | Optional catalog parameter; unit: `unitless`. |
| `EdgeFidelity` | `float` | `3` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Cutout` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
