# ShadowEffect Struct

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/ShadowEffect.cs`

Represents an immutable shadow effect value with an offset, blur radius, and color.

```csharp
public readonly record struct ShadowEffect
```

Inheritance:
`ValueType` -> `ShadowEffect`

## Examples

The following example creates a shadow effect and assigns it to a shape.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Media;

ShadowEffect shadow = new(
    new DrawPoint(4, 6),
    blurRadius: 12,
    color: new DrawColor(0, 0, 0, 128));

Rectangle rectangle = new()
{
    Shadow = shadow
};
```

## Remarks

`ShadowEffect` stores the data needed to describe a shadow: `Offset` moves the shadow relative to the rendered element, `BlurRadius` controls the blur amount, and `Color` provides the shadow color.

The constructor validates only `blurRadius`. It throws `ArgumentOutOfRangeException` when `blurRadius` is `NaN`, infinity, or negative. `Offset` and `Color` are stored as supplied; their own types define any construction-time validation they perform.

`ShadowEffect` is used by `Cerneala.UI.Controls.Shapes.Shape.Shadow`. The shape property is nullable, defaults to `null`, and is registered with `UiPropertyOptions.AffectsRender`, so changing it invalidates rendering for the shape.

Because the type is a `readonly record struct`, instances are immutable value types with value-based equality over `Offset`, `BlurRadius`, and `Color`.

## Constructors

| Name | Description |
| --- | --- |
| `ShadowEffect(DrawPoint offset, float blurRadius, DrawColor color)` | Initializes a shadow effect. Throws `ArgumentOutOfRangeException` when `blurRadius` is not finite or is negative. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Offset` | `DrawPoint` | Gets the offset applied to the shadow. |
| `BlurRadius` | `float` | Gets the finite, non-negative blur radius. |
| `Color` | `DrawColor` | Gets the shadow color. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(ShadowEffect)` | Determines whether another shadow effect has the same `Offset`, `BlurRadius`, and `Color` values. |
| `Equals(object?)` | Determines whether an object is an equivalent `ShadowEffect`. |
| `GetHashCode()` | Returns a hash code based on `Offset`, `BlurRadius`, and `Color`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==` | Determines whether two `ShadowEffect` values are equal. |
| `operator !=` | Determines whether two `ShadowEffect` values are not equal. |

## Applies To

Cerneala UI media values and shape rendering APIs.

## See Also

- `Cerneala.UI.Controls.Shapes.Shape`
- `Cerneala.Drawing.DrawPoint`
- `Cerneala.Drawing.DrawColor`
