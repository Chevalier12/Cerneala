# TwirlFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Twirl` filter.

```csharp
public sealed class TwirlFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TwirlFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Angle` | `float` | `50` | Optional catalog parameter; unit: `degrees`. |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Twirl` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
