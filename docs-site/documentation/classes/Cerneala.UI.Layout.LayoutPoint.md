# LayoutPoint Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/LayoutPoint.cs`

Represents an immutable two-dimensional layout point with `float` `X` and `Y` coordinates.

```csharp
public readonly record struct LayoutPoint(float X, float Y)
```

Inheritance:
`Object` -> `ValueType` -> `LayoutPoint`

Implements:
`IEquatable<LayoutPoint>`

## Examples

Create a layout point and use its component values to derive another point.

```csharp
using Cerneala.UI.Layout;

LayoutPoint origin = LayoutPoint.Zero;
LayoutPoint offset = new(origin.X + 12, origin.Y + 24);
```

Use a normalized layout point as a render transform origin.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIElement element = new()
{
    RenderTransformOrigin = new LayoutPoint(0.5f, 0.5f)
};
```

## Remarks

`LayoutPoint` stores an `X` coordinate and a `Y` coordinate for layout-related APIs. The type itself does not validate coordinate values; it stores the `float` values passed to its primary constructor.

`LayoutPoint.Zero` provides the `(0, 0)` point. `LayoutRect.Location` returns a `LayoutPoint` created from a rectangle's `X` and `Y` values, and `LayoutRounding.Round(LayoutPoint)` returns a new point with rounded coordinates when rounding is enabled.

The type is a `readonly record struct`, so values are immutable after construction and use value-based equality. The compiler provides record-struct members such as equality, deconstruction, hashing, and string formatting based on `X` and `Y`.

Some consumers may impose additional constraints. For example, `UIElement.RenderTransformOrigin` uses `LayoutPoint` as a normalized origin and accepts only finite coordinates between `0` and `1`.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutPoint(float X, float Y)` | Initializes a layout point with the specified horizontal and vertical coordinates. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `float` | Gets the horizontal coordinate. |
| `Y` | `float` | Gets the vertical coordinate. |
| `Zero` | `LayoutPoint` | Gets the `(0, 0)` layout point. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out float X, out float Y)` | Deconstructs the point into its `X` and `Y` components. |
| `Equals(LayoutPoint other)` | Determines whether another `LayoutPoint` has the same component values. |
| `GetHashCode()` | Returns a hash code based on `X` and `Y`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala UI layout primitives in the `Cerneala.UI.Layout` namespace.

## See also

- `Cerneala.UI.Layout.LayoutRect`
- `Cerneala.UI.Layout.LayoutRounding`
- `Cerneala.UI.Elements.UIElement.RenderTransformOrigin`
