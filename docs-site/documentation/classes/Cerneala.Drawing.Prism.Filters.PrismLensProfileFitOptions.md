# PrismLensProfileFitOptions Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Filters`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Filters/PrismLensProfileFitter.cs`

Controls deterministic sparse-polynomial lens-profile fitting.

```csharp
public sealed class PrismLensProfileFitOptions
```

## Examples

```csharp
PrismLensProfileFitOptions options = new()
{
    RegionCount = 6,
    MaximumTermCount = 16,
    PupilGridSize = 12
};
```

## Remarks

Higher region and term counts can improve difficult edge-of-field fits but
increase profile size and fitting cost. `PupilGridSize` controls runtime ghost
tessellation rather than the number of fitting samples.

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `RegionCount` | `int` | `4` | Gets or sets angular regions per ghost. |
| `MaximumTermCount` | `int` | `12` | Gets or sets the shared sparse basis limit. |
| `MinimumSamplesPerRegion` | `int` | `12` | Gets or sets the minimum fitting samples. |
| `PupilGridSize` | `int` | `8` | Gets or sets runtime pupil-grid resolution. |
| `Ridge` | `double` | `1e-7` | Gets or sets least-squares regularization. |
| `MinimumCorrelation` | `double` | `1e-7` | Gets or sets the basis-selection threshold. |

## See also

- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
