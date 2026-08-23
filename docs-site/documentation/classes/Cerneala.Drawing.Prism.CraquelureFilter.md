# CraquelureFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Craquelure` filter.

```csharp
public sealed class CraquelureFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `CraquelureFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CrackSpacing` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |
| `CrackDepth` | `float` | `6` | Optional catalog parameter; unit: `unitless`. |
| `CrackBrightness` | `float` | `9` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Craquelure` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
