# Matrix3x2 Struct

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/Matrix3x2.cs`

Represents an immutable 3x2 affine transformation matrix used to transform `DrawPoint` coordinates.

```csharp
public readonly record struct Matrix3x2
```

Inheritance:
`ValueType` -> `Matrix3x2`

Implements:
`IEquatable<Matrix3x2>`

## Examples
The following example creates a scale transform, then applies a translation after it.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

Matrix3x2 scale = Matrix3x2.CreateScale(2, 3);
Matrix3x2 translate = Matrix3x2.CreateTranslation(10, 20);
Matrix3x2 transform = Matrix3x2.Multiply(scale, translate);

DrawPoint result = transform.Transform(new DrawPoint(1, 2));
// result is (12, 26)
```

## Remarks
`Matrix3x2` stores affine transform values as six single-precision components:
`M11`, `M12`, `M21`, `M22`, `M31`, and `M32`.

`Transform(DrawPoint)` applies the matrix using the following formulas:

```text
x' = (x * M11) + (y * M21) + M31
y' = (x * M12) + (y * M22) + M32
```

`Multiply(left, right)` returns a matrix that applies `left` and then `right`, matching `Transform.Compose` behavior in `Transform.cs`.

The constructor and factory methods reject non-finite input values by throwing `ArgumentOutOfRangeException`. Matrix results are also created through the constructor, so operations can throw if the computed components are not finite.

Because this type is a `readonly record struct`, instances are immutable and use value-based equality.

## Constructors
| Name | Description |
| --- | --- |
| `Matrix3x2(float m11, float m12, float m21, float m22, float m31, float m32)` | Initializes a matrix with explicit component values. Throws `ArgumentOutOfRangeException` if any component is not finite. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Identity` | `Matrix3x2` | Gets the identity matrix `(1, 0, 0, 1, 0, 0)`. |
| `M11` | `float` | Gets the first row, first column component. |
| `M12` | `float` | Gets the first row, second column component. |
| `M21` | `float` | Gets the second row, first column component. |
| `M22` | `float` | Gets the second row, second column component. |
| `M31` | `float` | Gets the translation component used in the transformed X coordinate. |
| `M32` | `float` | Gets the translation component used in the transformed Y coordinate. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CreateTranslation(float x, float y)` | `Matrix3x2` | Creates a translation matrix with `x` and `y` stored in `M31` and `M32`. |
| `CreateScale(float x, float y)` | `Matrix3x2` | Creates a scale matrix with `x` in `M11` and `y` in `M22`. |
| `CreateRotation(float radians)` | `Matrix3x2` | Creates a rotation matrix using `MathF.Sin` and `MathF.Cos` for the supplied angle in radians. |
| `CreateSkew(float radiansX, float radiansY)` | `Matrix3x2` | Creates a skew matrix using `MathF.Tan(radiansX)` for `M21` and `MathF.Tan(radiansY)` for `M12`. |
| `Transform(DrawPoint point)` | `DrawPoint` | Applies the matrix to a point and returns the transformed point. |
| `Multiply(Matrix3x2 left, Matrix3x2 right)` | `Matrix3x2` | Multiplies two matrices and returns the composed transform. |

## Applies to
Project: `Cerneala`

## See also
- `Cerneala.UI.Media.Transform`
- `Cerneala.Drawing.DrawPoint`
