# CircleCollider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CircleCollider2D.cs`

Defines a circular collision shape in local scene coordinates.

```csharp
public sealed class CircleCollider2D : Collider2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Collider2D` -> `CircleCollider2D`

## Examples

```xml
<Scene2D TranslateX="120" TranslateY="80">
    <Sprite2D />
    <CircleCollider2D Radius="6" OffsetX="16" OffsetY="16" />
</Scene2D>
```

## Remarks

The local center is `(OffsetX, OffsetY)`. The radius and center are transformed through the collider and every ancestor `Scene2D` when scene-space geometry is requested.

`Radius` must remain finite and greater than zero. It has a float mixer and can be controlled by Aspect, bindings, direct assignment, or Motion. Each accepted change follows the same collision-geometry invalidation path.

## Constructors

| Name | Description |
| --- | --- |
| `CircleCollider2D()` | Creates an enabled circle collider with radius `1`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `RadiusProperty` | `UiProperty<float>` | Identifies the local radius UI property. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `1` | Gets or sets the finite positive local radius. |

## Exceptions

Setting `Radius` to zero, a negative value, `NaN`, or infinity throws `ArgumentOutOfRangeException`.

## Applies to

Project: `Cerneala`

## See also

- `Collider2D`
- `BoxCollider2D`
- `PolygonCollider2D`

