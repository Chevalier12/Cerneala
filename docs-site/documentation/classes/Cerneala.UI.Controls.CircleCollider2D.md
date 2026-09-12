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
<Sprite2D X="120" Y="80">
    <CircleCollider2D Radius="6" OffsetX="16" OffsetY="16" />
</Sprite2D>
```

## Remarks

The local center is `(OffsetX, OffsetY)`. The radius and center are transformed through the collider and every ancestor `Scene2D` when scene-space geometry is requested.

Live circles attach only through `Sprite2D.Colliders` or `TileInstance2D.Colliders`, following the owner's pose. The example supplies collision geometry; assign the sprite's `Image` separately for drawing. Under a static `Tile`, the same shape name authors an immutable descriptor with literal values. See [Collider2D](Cerneala.UI.Controls.Collider2D.md) for ownership and independent shape sizing.

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
