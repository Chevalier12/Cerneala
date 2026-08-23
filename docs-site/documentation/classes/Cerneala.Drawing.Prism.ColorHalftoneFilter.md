# ColorHalftoneFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColorHalftone` filter.

```csharp
public sealed class ColorHalftoneFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColorHalftoneFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `MaxRadius` | `float` | `4` | Optional catalog parameter; unit: `dip`. |
| `Angles` | `Vector4` | `108, 162, 90, 45` | Optional catalog parameter; unit: `degrees`. |

## Remarks

Parameter assignments are validated against the `ColorHalftone` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
