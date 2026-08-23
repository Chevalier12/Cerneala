# ChromaticAberrationFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ChromaticAberration` filter.

```csharp
public sealed class ChromaticAberrationFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ChromaticAberrationFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Direction` | `Vector4` | `1, 0` | Optional catalog parameter; unit: `unitless`. |
| `Radial` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Sampling` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ChromaticAberration` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
