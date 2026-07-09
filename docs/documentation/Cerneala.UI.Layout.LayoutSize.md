# LayoutSize Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/LayoutSize.cs`

Represents an immutable two-dimensional layout size with `float` `Width` and `Height` components.

```csharp
public readonly record struct LayoutSize(float Width, float Height)
```

Inheritance:
`Object` -> `ValueType` -> `LayoutSize`

Implements:
`IEquatable<LayoutSize>`

## Examples

Create fixed, zero, and unconstrained layout sizes.

```csharp
using Cerneala.UI.Layout;

LayoutSize fixedSize = new(320, 180);
LayoutSize empty = LayoutSize.Zero;
LayoutSize unconstrained = LayoutSize.Unconstrained;

bool widthCanGrow = unconstrained.IsWidthUnconstrained;
```

Clamp negative components before returning a measured size.

```csharp
using Cerneala.UI.Layout;

LayoutSize measured = new(-12, 24).ClampNonNegative();
// measured is LayoutSize(0, 24)
```

## Remarks

`LayoutSize` stores width and height values for layout APIs such as available size, desired size, and rectangle size composition. The type stores the `float` values passed to its primary constructor; it does not validate, round, normalize, or clamp values during construction.

`Zero` provides the `(0, 0)` size. `Unconstrained` uses `float.PositiveInfinity` for both dimensions. `IsWidthUnconstrained` and `IsHeightUnconstrained` report whether the corresponding component is positive infinity.

Use `ClampNonNegative()` when a size must not contain negative components. It returns a new `LayoutSize` whose `Width` and `Height` are each clamped with `MathF.Max(0, value)`.

Because `LayoutSize` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as equality, deconstruction, hashing, and string formatting based on `Width` and `Height`.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutSize(float Width, float Height)` | Initializes a layout size with the specified width and height components. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Width` | `float` | Gets the width component. |
| `Height` | `float` | Gets the height component. |
| `Zero` | `LayoutSize` | Gets the `(0, 0)` layout size. |
| `Unconstrained` | `LayoutSize` | Gets a size whose `Width` and `Height` are both `float.PositiveInfinity`. |
| `IsWidthUnconstrained` | `bool` | Gets whether `Width` is positive infinity. |
| `IsHeightUnconstrained` | `bool` | Gets whether `Height` is positive infinity. |

## Methods

| Name | Description |
| --- | --- |
| `ClampNonNegative()` | Returns a new `LayoutSize` with negative `Width` or `Height` values replaced by `0`. |
| `Deconstruct(out float Width, out float Height)` | Deconstructs the size into its `Width` and `Height` components. |
| `Equals(LayoutSize other)` | Determines whether another `LayoutSize` has the same component values. |
| `GetHashCode()` | Returns a hash code based on `Width` and `Height`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala UI layout primitives in the `Cerneala.UI.Layout` namespace.

## See also

- `Cerneala.UI.Layout.LayoutPoint`
- `Cerneala.UI.Layout.LayoutRect`
- `Cerneala.UI.Elements.UIElement.DesiredSize`
