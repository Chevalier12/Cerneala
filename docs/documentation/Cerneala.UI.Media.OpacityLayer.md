# OpacityLayer Struct

## Definition
Namespace: `Cerneala.UI.Media`

Assembly/Project: `Cerneala`

Source: `UI/Media/OpacityLayer.cs`

Represents a normalized opacity value for a media layer.

```csharp
public readonly record struct OpacityLayer
```

Inheritance:
`ValueType` -> `OpacityLayer`

## Examples

The following example creates an opacity layer at 50% opacity.

```csharp
using Cerneala.UI.Media;

OpacityLayer layer = new(0.5f);

float opacity = layer.Opacity;
```

## Remarks

`OpacityLayer` stores one immutable opacity value. Valid opacity values are finite `float` values from `0` through `1`, where `0` is fully transparent and `1` is fully opaque.

The constructor rejects `NaN`, infinity, negative values, and values greater than `1` by throwing `ArgumentOutOfRangeException`.

Because the type is a `readonly record struct`, instances are value types with value-based equality over `Opacity`.

## Constructors

| Name | Description |
| --- | --- |
| `OpacityLayer(float opacity)` | Initializes an opacity layer with a normalized opacity value. Throws `ArgumentOutOfRangeException` when `opacity` is not finite or is outside `0` to `1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Opacity` | `float` | Gets the normalized opacity value. Valid values are finite numbers from `0` through `1`. |

## Methods

| Name | Description |
| --- | --- |
| `Equals(OpacityLayer)` | Determines whether another opacity layer has the same `Opacity` value. |
| `Equals(object?)` | Determines whether an object is an equivalent `OpacityLayer`. |
| `GetHashCode()` | Returns a hash code based on the opacity value. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==` | Determines whether two `OpacityLayer` values are equal. |
| `operator !=` | Determines whether two `OpacityLayer` values are not equal. |

## Applies To

Cerneala UI media and rendering APIs.

## See Also

- `Cerneala.UI.Rendering.RenderLayer`
