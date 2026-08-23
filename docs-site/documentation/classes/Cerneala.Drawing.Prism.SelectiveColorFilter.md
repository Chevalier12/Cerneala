# SelectiveColorFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SelectiveColor` filter.

```csharp
public sealed class SelectiveColorFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SelectiveColorFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Reds` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Yellows` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Greens` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Cyans` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Blues` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Magentas` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Whites` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Neutrals` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Blacks` | `Vector4` | `0, 0, 0, 0` | Optional catalog parameter; unit: `unitless`. |
| `Method` | `string` | `Relative` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `SelectiveColor` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
