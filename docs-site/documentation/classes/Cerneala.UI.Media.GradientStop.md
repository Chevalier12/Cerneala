# GradientStop Struct

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/GradientStop.cs`

Represents a color stop in a gradient brush at a normalized offset.

```csharp
public readonly record struct GradientStop
```

Inheritance:
`ValueType` -> `GradientStop`

## Examples

The following example creates a left-to-right linear gradient from white to black.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

LinearGradientBrush brush = new(
    new DrawPoint(0, 0),
    new DrawPoint(100, 0),
    [
        new GradientStop(0f, DrawColor.White),
        new GradientStop(1f, DrawColor.Black)
    ]);
```

## Remarks

`GradientStop` stores the color and position used by `LinearGradientBrush` and `RadialGradientBrush`.
The `Offset` value is normalized: `0` represents the start of the gradient range, and `1` represents the end.

The constructor rejects offsets that are not finite or are outside the inclusive `0` to `1` range. Because the type is a `readonly record struct`, instances are value types with value-based equality over `Offset` and `Color`.

Gradient brushes order their exposed stop collections by `Offset`, so callers do not need to pass stops in sorted order.

## Constructors

| Name | Description |
| --- | --- |
| `GradientStop(float offset, DrawColor color)` | Initializes a gradient stop with a normalized offset and color. Throws `ArgumentOutOfRangeException` when `offset` is not finite or is outside `0` to `1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Offset` | `float` | Gets the normalized gradient position. Valid values are finite numbers from `0` through `1`. |
| `Color` | `DrawColor` | Gets the color applied at `Offset`. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(GradientStop)` | Determines whether another stop has the same `Offset` and `Color`. |
| `Equals(object?)` | Determines whether an object is an equivalent `GradientStop`. |
| `GetHashCode()` | Returns a hash code based on the stop value. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==` | Determines whether two `GradientStop` values are equal. |
| `operator !=` | Determines whether two `GradientStop` values are not equal. |

## Applies to

Cerneala UI media gradient brushes.

## See also

- `Cerneala.UI.Media.LinearGradientBrush`
- `Cerneala.UI.Media.RadialGradientBrush`
- `Cerneala.Drawing.DrawColor`
