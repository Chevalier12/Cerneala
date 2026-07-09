# DrawArgument Class

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `UI/Drawing/DrawArgument.cs`

Provides internal validation helpers for drawing coordinates, pixel sizes, text sizes, and finite numeric arguments.

```csharp
internal static class DrawArgument
```

## Examples

```csharp
DrawArgument.ThrowIfNotValidPixelCoordinate(x, nameof(x));
DrawArgument.ThrowIfNegativeOrNotValidPixelSize(width, nameof(width));
DrawArgument.ThrowIfNotValidTextSize(fontSize, nameof(fontSize));
```

## Remarks

`DrawArgument` centralizes guard checks used by drawing primitives. The helpers throw `ArgumentOutOfRangeException` when a value is not finite, negative where negative values are invalid, outside the supported pixel coordinate range, outside the supported pixel size range, or outside the supported text size range.

Pixel coordinates are constrained to the inclusive range `-2_000_000_000` through `2_000_000_000`. Pixel sizes are constrained to positive or non-negative values depending on the helper and cannot exceed `2_000_000_000`. Text sizes must be positive, finite, and no greater than `16_384`.

## Methods

| Name | Description |
| --- | --- |
| `ThrowIfNotFinite(float, string)` | Throws when the value is not finite. |
| `ThrowIfNegativeOrNotFinite(float, string)` | Throws when the value is negative or not finite. |
| `ThrowIfNotValidPixelCoordinate(float, string)` | Throws when the coordinate is not finite or is outside the supported pixel coordinate range. |
| `ThrowIfNegativeOrNotValidPixelSize(float, string)` | Throws when the pixel size is negative, not finite, or larger than the maximum pixel size. |
| `ThrowIfNotPositiveFinite(float, string)` | Throws when the value is less than or equal to zero or not finite. |
| `ThrowIfNotValidPixelSize(float, string)` | Throws when the pixel size is not positive, not finite, or larger than the maximum pixel size. |
| `ThrowIfNotValidTextSize(float, string)` | Throws when the text size is not positive, not finite, or larger than the maximum text size. |

## Applies to

Cerneala drawing internals.

## See also

- `Cerneala.Drawing.DrawRect`
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawingContext`
