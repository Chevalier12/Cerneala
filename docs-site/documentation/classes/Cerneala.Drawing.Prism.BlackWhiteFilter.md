# BlackWhiteFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `BlackWhite` filter.

```csharp
public sealed class BlackWhiteFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `BlackWhiteFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Red` | `float` | `0.333` | Optional catalog parameter; unit: `unitless`. |
| `Green` | `float` | `0.333` | Optional catalog parameter; unit: `unitless`. |
| `Blue` | `float` | `0.333` | Optional catalog parameter; unit: `unitless`. |
| `PreserveLuminosity` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `BlackWhite` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
