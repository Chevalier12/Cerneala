# DrawSize Struct

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawSize.cs`

Represents an immutable drawing-space size with finite `float` width and height components.

```csharp
public readonly record struct DrawSize
```

Inheritance:
`Object` -> `ValueType` -> `DrawSize`

Implements:
`IEquatable<DrawSize>`

## Examples

Create a size value and read its components:

```csharp
using Cerneala.Drawing;

DrawSize size = new(120, 48);

float width = size.Width;
float height = size.Height;
```

Use `DrawSize` with the built-in motion mixer:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

DrawSizeMixer mixer = new();

DrawSize from = new(10, 20);
DrawSize to = new(30, 60);
DrawSize halfway = mixer.Mix(from, to, 0.5f);
```

## Remarks

`DrawSize` stores width and height values for drawing-related APIs. The constructor requires both components to be finite; `NaN`, positive infinity, and negative infinity are rejected with `ArgumentOutOfRangeException`.

Unlike `DrawRect`, `DrawSize` does not require non-negative dimensions or clamp values to the drawing pixel range. Code that consumes a `DrawSize` can apply more specific validation for its own context.

The type is a `readonly record struct`, so values are immutable after construction and use value-based equality. As with any struct, `default(DrawSize)` is valid and represents a size with `Width` and `Height` equal to `0`.

`DrawSize` is also supported by `DrawSizeMixer`, which provides interpolation and vector operations for motion values.

## Constructors

| Name | Description |
| --- | --- |
| `DrawSize(float width, float height)` | Initializes a size from finite `width` and `height` values. Throws `ArgumentOutOfRangeException` when either argument is not finite. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Width` | `float` | Gets the width component. |
| `Height` | `float` | Gets the height component. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DrawSize(float width, float height)` | `ArgumentOutOfRangeException` | `width` or `height` is `NaN`, positive infinity, or negative infinity. |

## Applies To

Cerneala drawing primitives and motion interpolation APIs that consume `Cerneala.Drawing.DrawSize`.

## See Also

- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawRect`
- `Cerneala.UI.Motion.Interpolation.DrawSizeMixer`
