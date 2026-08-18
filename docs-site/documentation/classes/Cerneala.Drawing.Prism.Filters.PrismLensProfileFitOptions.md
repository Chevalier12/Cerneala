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

The properties are init-only and are validated when `PrismLensProfileFitter.Fit`
is called. The fitter rejects options outside the supported ranges before it
consumes the samples.

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `RegionCount` | `int` | `4` | Gets the number of angular regions per ghost; valid range is 1 through 32. |
| `MaximumTermCount` | `int` | `12` | Gets the shared sparse basis limit; valid range is 1 through 28. |
| `MinimumSamplesPerRegion` | `int` | `12` | Gets the minimum fitting samples per region; must be at least 6. |
| `PupilGridSize` | `int` | `8` | Gets the runtime pupil-grid resolution; valid range is 2 through 32. |
| `Ridge` | `double` | `1e-7` | Gets the positive, finite least-squares regularization value. |
| `MinimumCorrelation` | `double` | `1e-7` | Gets the finite, non-negative basis-selection threshold. |

## Exceptions

| Operation | Exception | Condition |
| --- | --- | --- |
| `PrismLensProfileFitter.Fit` | `ArgumentOutOfRangeException` | An option is outside its supported range, is not finite, or is not positive where required. |

## See also

- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
