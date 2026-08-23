# ChromeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Chrome` filter.

```csharp
public sealed class ChromeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ChromeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Detail` | `float` | `4` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `7` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Chrome` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
