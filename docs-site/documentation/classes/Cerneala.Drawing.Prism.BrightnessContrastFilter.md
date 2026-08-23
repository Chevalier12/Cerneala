# BrightnessContrastFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `BrightnessContrast` filter.

```csharp
public sealed class BrightnessContrastFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `BrightnessContrastFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Brightness` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `UseLegacy` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `BrightnessContrast` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
