# RadialGradientBrush Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/RadialGradientBrush.cs`

Represents an immutable radial gradient brush defined by a center point, horizontal and vertical radii, and one or more gradient stops.

```csharp
public sealed record RadialGradientBrush : Brush
```

Inheritance:
`object` -> `Brush` -> `RadialGradientBrush`

## Examples

The following example creates an elliptical radial gradient brush with two stops.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

RadialGradientBrush brush = new(
    new DrawPoint(50, 50),
    40,
    24,
    [
        new GradientStop(0f, DrawColor.White),
        new GradientStop(1f, DrawColor.Black)
    ]);
```

## Remarks

`RadialGradientBrush` stores the geometry and color stops for a radial gradient. `Center` identifies the gradient center in drawing coordinates. `RadiusX` and `RadiusY` define the horizontal and vertical radii and must both be finite values greater than zero.

The constructor orders the supplied stops by `GradientStop.Offset` before exposing them through `Stops`. The stop collection must contain at least one item. Passing `null` for `stops` throws `ArgumentNullException`; passing an empty sequence throws `ArgumentException`.

Because `RadialGradientBrush` derives from `Brush` and does not override `SolidColor`, its inherited `SolidColor` property returns `null`. Consumers that require a concrete `DrawColor` should treat this brush as a non-solid brush.

Equality compares `Center`, `RadiusX`, `RadiusY`, and the ordered stop sequence. Hash codes are computed from the same values, so two radial gradients with the same stops in a different input order compare equal after construction.

## Constructors

| Name | Description |
| --- | --- |
| `RadialGradientBrush(DrawPoint center, float radiusX, float radiusY, IEnumerable<GradientStop> stops)` | Initializes a radial gradient brush and orders `stops` by offset. Throws `ArgumentOutOfRangeException` when either radius is not finite or is less than or equal to zero, `ArgumentNullException` when `stops` is `null`, and `ArgumentException` when `stops` is empty. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Center` | `DrawPoint` | Gets the center point of the radial gradient. |
| `RadiusX` | `float` | Gets the horizontal radius of the radial gradient. |
| `RadiusY` | `float` | Gets the vertical radius of the radial gradient. |
| `Stops` | `IReadOnlyList<GradientStop>` | Gets the gradient stops ordered by `GradientStop.Offset`. |
| `SolidColor` | `DrawColor?` | Inherited from `Brush`; returns `null` for `RadialGradientBrush`. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(RadialGradientBrush?)` | Determines whether another radial gradient brush has the same center, radii, and ordered stop sequence. |
| `Equals(object?)` | Determines whether an object is an equivalent `RadialGradientBrush`. |
| `GetHashCode()` | Returns a hash code based on `Center`, `RadiusX`, `RadiusY`, and `Stops`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==` | Determines whether two `RadialGradientBrush` instances are equal. |
| `operator !=` | Determines whether two `RadialGradientBrush` instances are not equal. |

## Applies to

Cerneala UI media gradient brushes and retained rendering APIs.

## See also

- `Cerneala.UI.Media.Brush`
- `Cerneala.UI.Media.GradientStop`
- `Cerneala.UI.Media.LinearGradientBrush`
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawColor`
