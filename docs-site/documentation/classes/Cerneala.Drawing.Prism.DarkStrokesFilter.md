# DarkStrokesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DarkStrokes` filter.

```csharp
public sealed class DarkStrokesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DarkStrokesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Balance` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |
| `BlackIntensity` | `float` | `6` | Optional catalog parameter; unit: `unitless`. |
| `WhiteIntensity` | `float` | `2` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `DarkStrokes` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
