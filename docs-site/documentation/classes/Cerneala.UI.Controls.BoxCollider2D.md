# BoxCollider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/BoxCollider2D.cs`

Defines a rectangular collision shape in local scene coordinates.

```csharp
public sealed class BoxCollider2D : Collider2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Collider2D` -> `BoxCollider2D`

## Examples

```xml
<Scene2D TranslateX="96" TranslateY="48">
    <Sprite2D />
    <BoxCollider2D Width="32" Height="8" OffsetY="24">
        <BoxCollider2D.Aspect>
            @on Loaded
            {
                @animate with Tween(100ms)
                {
                    @to { OffsetX = 2; Width = 30; Height = 8; }
                }
            }
        </BoxCollider2D.Aspect>
    </BoxCollider2D>
</Scene2D>
```

## Remarks

The box starts at `(OffsetX, OffsetY)` and extends by `Width` and `Height` before inherited collider and group transforms are applied. Both dimensions must remain finite and greater than zero.

`Width` and `Height` are collision dimensions, not UI layout dimensions. Both properties have float mixers and can be controlled by Aspect, bindings, direct assignment, or Motion. Each accepted change follows the same collision-geometry invalidation path.

## Constructors

| Name | Description |
| --- | --- |
| `BoxCollider2D()` | Creates a `1` by `1` enabled box collider. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `WidthProperty` | `UiProperty<float>` | Identifies the collision width UI property. |
| `HeightProperty` | `UiProperty<float>` | Identifies the collision height UI property. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Width` | `float` | `1` | Gets or sets the finite positive local width. |
| `Height` | `float` | `1` | Gets or sets the finite positive local height. |

## Exceptions

Setting either dimension to zero, a negative value, `NaN`, or infinity throws `ArgumentOutOfRangeException`.

## Applies to

Project: `Cerneala`

## See also

- `Collider2D`
- `CircleCollider2D`
- `PolygonCollider2D`
