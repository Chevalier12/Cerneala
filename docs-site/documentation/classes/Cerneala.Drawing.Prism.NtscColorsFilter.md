# NtscColorsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `NtscColors` filter.

```csharp
public sealed class NtscColorsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `NtscColorsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Standard` | `string` | `NTSC` | Optional catalog parameter; unit: `none`. |
| `Method` | `string` | `ReduceLuminance` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `NtscColors` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
