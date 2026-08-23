# PhotoFilterFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PhotoFilter` filter.

```csharp
public sealed class PhotoFilterFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PhotoFilterFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Color` | `Color` | `#FFFF9A30` | Optional catalog parameter; unit: `none`. |
| `Density` | `float` | `0.25` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `PhotoFilter` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
