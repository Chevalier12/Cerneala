# LinearGradientBrush Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/LinearGradientBrush.cs`

Represents an immutable linear gradient brush defined by start and end points plus one or more gradient stops.

```csharp
public sealed record LinearGradientBrush : Brush
```

Inheritance:
`object` -> `Brush` -> `LinearGradientBrush`

## Examples

The following example creates a left-to-right gradient brush with two stops.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

LinearGradientBrush brush = new(
    new DrawPoint(0, 0),
    new DrawPoint(200, 0),
    [
        new GradientStop(0f, DrawColor.White),
        new GradientStop(1f, DrawColor.Black)
    ]);
```

## Remarks

`LinearGradientBrush` stores the geometry and color stops for a linear gradient. `StartPoint` and `EndPoint` describe the gradient line in drawing coordinates, while `Stops` contains the colors to apply along the normalized gradient range.

The constructor orders the supplied stops by `GradientStop.Offset` before exposing them through `Stops`. The stop collection must contain at least one item. Passing `null` for `stops` throws `ArgumentNullException`; passing an empty sequence throws `ArgumentException`.

Because `LinearGradientBrush` derives from `Brush` and does not override `SolidColor`, its inherited `SolidColor` property returns `null`. Consumers that require a concrete `DrawColor` should treat this brush as a non-solid brush.

Equality compares `StartPoint`, `EndPoint`, and the ordered stop sequence. Hash codes are computed from the same values.

## Constructors

| Name | Description |
| --- | --- |
| `LinearGradientBrush(DrawPoint startPoint, DrawPoint endPoint, IEnumerable<GradientStop> stops)` | Initializes a linear gradient brush and orders `stops` by offset. Throws `ArgumentNullException` when `stops` is `null`, and `ArgumentException` when `stops` is empty. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `StartPoint` | `DrawPoint` | Gets the start point of the gradient line. |
| `EndPoint` | `DrawPoint` | Gets the end point of the gradient line. |
| `Stops` | `IReadOnlyList<GradientStop>` | Gets the gradient stops ordered by `GradientStop.Offset`. |
| `SolidColor` | `DrawColor?` | Inherited from `Brush`; returns `null` for `LinearGradientBrush`. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(LinearGradientBrush?)` | Determines whether another linear gradient brush has the same start point, end point, and ordered stop sequence. |
| `Equals(object?)` | Determines whether an object is an equivalent `LinearGradientBrush`. |
| `GetHashCode()` | Returns a hash code based on `StartPoint`, `EndPoint`, and `Stops`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==` | Determines whether two `LinearGradientBrush` instances are equal. |
| `operator !=` | Determines whether two `LinearGradientBrush` instances are not equal. |

## Applies to

Cerneala UI media gradient brushes and retained rendering APIs.

## See also

- `Cerneala.UI.Media.Brush`
- `Cerneala.UI.Media.GradientStop`
- `Cerneala.UI.Media.RadialGradientBrush`
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawColor`
