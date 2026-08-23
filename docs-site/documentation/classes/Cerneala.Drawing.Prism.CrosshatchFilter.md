# CrosshatchFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Crosshatch` filter.

```csharp
public sealed class CrosshatchFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `CrosshatchFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeLength` | `float` | `9` | Optional catalog parameter; unit: `dip`. |
| `Sharpness` | `float` | `6` | Optional catalog parameter; unit: `unitless`. |
| `Strength` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Crosshatch` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
