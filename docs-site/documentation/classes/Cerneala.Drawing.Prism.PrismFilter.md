# PrismFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/PrismOperation.cs`

Provides common state for generated code-defined Prism filters.

```csharp
public abstract class PrismFilter : PrismOperation
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FilterId` | `PrismFilterId` | Gets the stable Prism catalog filter identifier. |
| `Visible` | `bool` | Gets or sets whether the filter participates in rendering. |
| `Opacity` | `float` | Gets or sets filter opacity from `0` through `1`. |
| `BlendMode` | `PrismBlendMode` | Gets or sets the filter blend mode. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Opacity` | `ArgumentOutOfRangeException` | The value is not finite or is outside `0` through `1`. |
| `BlendMode` | `ArgumentException` | The value is `PassThrough`, which is valid only for groups. |

## See Also

- `PrismOperation`
- `PrismStyle`
- `PrismPipeline`
