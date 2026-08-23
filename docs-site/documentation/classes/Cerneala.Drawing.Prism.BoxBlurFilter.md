# BoxBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `BoxBlur` filter.

```csharp
public sealed class BoxBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `BoxBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `Iterations` | `float` | `1` | Optional catalog parameter; unit: `count`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `BoxBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
