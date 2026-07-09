# UiCoordinateMapper Class

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/UiCoordinateMapper.cs`

Converts Cerneala UI coordinates between logical units and physical pixels for scaled hosts and backends.

```csharp
public static class UiCoordinateMapper
```

Inheritance:
`object` -> `UiCoordinateMapper`

## Examples

Convert a logical coordinate to a physical coordinate:

```csharp
using Cerneala.UI.Hosting;

float physicalX = UiCoordinateMapper.LogicalToPhysical(logical: 120.5f, scale: 2f);
```

Convert a physical pointer coordinate back into logical UI space:

```csharp
using Cerneala.UI.Hosting;

float logicalX = UiCoordinateMapper.PhysicalToLogical(physical: 101f, scale: 2f);
```

Round a logical coordinate to a physical pixel boundary:

```csharp
using Cerneala.UI.Hosting;

int pixel = UiCoordinateMapper.LogicalToPhysicalPixel(logical: 10.25f, scale: 2f);
```

## Remarks

`UiCoordinateMapper` centralizes the scale math used by hosting, input, and MonoGame drawing code. Logical coordinates are multiplied by `scale` when converting to physical units, and physical coordinates are divided by `scale` when converting back to logical units.

`LogicalToPhysicalPixel` first maps the logical value to physical units, then rounds with `MidpointRounding.AwayFromZero`. This gives deterministic pixel conversion for fractional physical coordinates, including half-pixel results.

All public methods reject non-finite coordinates. The `scale` argument must be finite and greater than zero. Invalid coordinates or scale values throw `ArgumentOutOfRangeException`.

`UiViewport.FromPhysicalPixels` uses this mapper to derive logical viewport dimensions from physical pixel dimensions. MonoGame input uses it to report pointer positions in logical UI space, while MonoGame drawing uses it to map logical rectangles, vectors, thicknesses, and text sizes to physical rendering coordinates.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `LogicalToPhysical(float logical, float scale)` | `float` | Multiplies a finite logical coordinate by a positive finite scale. Throws `ArgumentOutOfRangeException` when `logical` is not finite or `scale` is not finite or is less than or equal to zero. |
| `PhysicalToLogical(float physical, float scale)` | `float` | Divides a finite physical coordinate by a positive finite scale. Throws `ArgumentOutOfRangeException` when `physical` is not finite or `scale` is not finite or is less than or equal to zero. |
| `LogicalToPhysicalPixel(float logical, float scale)` | `int` | Converts a logical coordinate to physical units and rounds the result to an integer pixel using `MidpointRounding.AwayFromZero`. Throws `ArgumentOutOfRangeException` for the same invalid inputs as `LogicalToPhysical`. |

## Exceptions

| Method | Exception | Condition |
| --- | --- | --- |
| `LogicalToPhysical(float, float)` | `ArgumentOutOfRangeException` | `logical` is `NaN` or infinite, or `scale` is `NaN`, infinite, zero, or negative. |
| `PhysicalToLogical(float, float)` | `ArgumentOutOfRangeException` | `physical` is `NaN` or infinite, or `scale` is `NaN`, infinite, zero, or negative. |
| `LogicalToPhysicalPixel(float, float)` | `ArgumentOutOfRangeException` | `logical` is `NaN` or infinite, or `scale` is `NaN`, infinite, zero, or negative. |

## Applies to

Cerneala UI hosting, MonoGame input mapping, and MonoGame drawing coordinate scaling.

## See also

- `Cerneala.UI.Hosting.UiViewport`
- `Cerneala.UI.Hosting.UiHost`
- `Cerneala.UI.Input.MonoGame.MonoGameInputSource`
- `Cerneala.Drawing.MonoGame.MonoGameDrawingBackend`
