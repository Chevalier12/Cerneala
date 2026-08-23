# WindFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Wind` filter.

```csharp
public sealed class WindFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `WindFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Method` | `string` | `Wind` | Optional catalog parameter; unit: `none`. |
| `Direction` | `string` | `FromRight` | Optional catalog parameter; unit: `none`. |
| `Strength` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Wind` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
