# ExtrudeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Extrude` filter.

```csharp
public sealed class ExtrudeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ExtrudeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Type` | `string` | `Blocks` | Optional catalog parameter; unit: `none`. |
| `Size` | `float` | `30` | Optional catalog parameter; unit: `dip`. |
| `Depth` | `float` | `30` | Optional catalog parameter; unit: `unitless`. |
| `DepthMode` | `string` | `Random` | Optional catalog parameter; unit: `none`. |
| `SolidFrontFaces` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `MaskIncompleteBlocks` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Extrude` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
