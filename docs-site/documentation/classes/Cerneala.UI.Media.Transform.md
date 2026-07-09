# Transform Class

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/Transform.cs`

Represents an immutable two-dimensional transform backed by a `Matrix3x2`.

```csharp
public sealed record Transform(Matrix3x2 Matrix)
```

Inheritance:
`object` -> `Transform`

## Examples

Apply and compose transforms:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Media;

Transform scale = new(Matrix3x2.CreateScale(2, 3));
Transform translate = new(Matrix3x2.CreateTranslation(10, 20));

Transform composed = scale.Compose(translate);
DrawPoint result = composed.Apply(new DrawPoint(1, 2));
```

## Remarks

`Transform` is a small immutable wrapper around `Matrix3x2`. `Identity` returns a transform backed by `Matrix3x2.Identity`, and `Apply` transforms a point by delegating to the stored matrix.

`Compose` combines the current transform with the `next` transform by multiplying the current matrix with `next.Matrix`. Passing `null` for `next` throws `ArgumentNullException`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Matrix` | `Matrix3x2` | Gets the matrix backing the transform. |
| `Identity` | `Transform` | Gets an identity transform that leaves points unchanged. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Apply(DrawPoint point)` | `DrawPoint` | Applies the transform matrix to a point. |
| `Compose(Transform next)` | `Transform` | Returns a new transform composed from the current transform and `next`. |

## Applies To

Cerneala retained UI media, rendering, and motion APIs.

## See Also

- `Cerneala.UI.Media.Matrix3x2`
- `Cerneala.Drawing.DrawPoint`
