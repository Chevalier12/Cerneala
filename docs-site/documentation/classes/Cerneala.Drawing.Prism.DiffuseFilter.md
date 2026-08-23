# DiffuseFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Diffuse` filter.

```csharp
public sealed class DiffuseFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DiffuseFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Mode` | `string` | `Normal` | Optional catalog parameter; unit: `none`. |
| `Iterations` | `float` | `1` | Optional catalog parameter; unit: `count`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Diffuse` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
