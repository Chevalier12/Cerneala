# ColorLookupFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColorLookup` filter.

```csharp
public sealed class ColorLookupFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColorLookupFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Lookup` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ColorLookup` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
