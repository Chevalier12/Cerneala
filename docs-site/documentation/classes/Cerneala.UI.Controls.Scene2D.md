# Scene2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2D.cs`

Groups retained 2D scene nodes in deterministic drawing order.

```csharp
[ContentProperty(nameof(Children))]
public sealed class Scene2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Scene2D`

## Examples

```xml
<RenderSurface2D.Scene>
    <Scene2D OrderMode="LayerThenY"
             TranslateX="32"
             Scale="1.5"
             Rotation="0.1"
             TransformOrigin="128,96">
        <Sprite2D Layer="0" />
        <Sprite2D Layer="10" />
    </Scene2D>
</RenderSurface2D.Scene>
```

## Remarks

Direct markup children are added to `Children`. `OrderMode="Source"`, the default, records them from first to last, so later children draw after earlier children. Adding, replacing, removing, or clearing children invalidates the owning surface.

`OrderMode="Layer"` sorts by each child's `Layer` value. `OrderMode="LayerThenY"` sorts first by `Layer`, then by the bottom edge of the child's transformed scene-space bounds. Smaller values draw first. Both modes are stable: equal keys retain source collection order, and neither mode mutates `Children`. A missing or unknown bound uses a Y anchor of `0` rather than removing the node.

Picking uses that same effective order in reverse, so the last visible eligible node at a point wins. The chosen node enters the ordinary UI route through its scene ancestors and the owning `RenderSurface2D`; scene groups do not create a second router or event family.

Children belong to the logical tree and inherit data context and attachment state. Setting either `IsVisible` to `false` or `Visibility` to a non-visible value skips the group and all of its descendants.

The root group owns a `CollisionWorld2D`. `CollisionWorld` on any nested group resolves to the same root-owned world. Structural and collider-property mutations update that world incrementally; removing a subtree removes its indexed colliders before the next query.

`Scene2D` applies the inherited `Scale`, `ScaleX`, `ScaleY`, `SkewX`, `SkewY`, `Rotation`, `TranslateX`, `TranslateY`, and `RenderTransform` channels to the entire descendant group. `TransformOrigin` is an absolute point in the group's local scene coordinates, not a normalized layout point. The local transform is composed in this order: translate away from the origin, scale, skew, rotate, translate, apply `RenderTransform`, and translate back to the origin. Nested groups compose their transforms from child to parent.

These transform channels and `TransformOrigin` are `UiProperty` values, so Aspect and Motion can control them. A group `Opacity` scopes its descendants. Prism attached to a group captures exactly that group's descendant commands and uses their aggregate transformed scene bounds; Prism attached to a child remains nested inside the group scope.

Aspect can also assign the structural `OrderMode` and inherited `Layer` properties. Motion deliberately rejects those two properties because no ordering mixer or interpolation contract exists. Animating `TranslateY`, other transform channels, or `Opacity` remains supported and updates ordering or presentation in the same logical frame.

A transform with no inverse, such as `ScaleX="0"`, still records the group. Forward rendering and conservative bounds remain available, while world-to-local conversion is unavailable internally for that transform.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2D()` | Creates an empty owned child collection. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `Collection<SceneNode2D>` | Ordered retained nodes recorded by the group. |
| `CollisionWorld` | `CollisionWorld2D` | Root-owned collision query world shared by this group and its nested scene groups. |
| `OrderMode` | `SceneOrderMode` | Selects source, layer, or stable layer-then-Y recording order. The default is `Source`. |
| `TransformOrigin` | `DrawPoint` | Absolute transform pivot in local scene-space units. The default is `(0, 0)`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrderModeProperty` | `UiProperty<SceneOrderMode>` | Identifies the `OrderMode` UI property. Changes affect rendering and ordering. |
| `TransformOriginProperty` | `UiProperty<DrawPoint>` | Identifies the `TransformOrigin` UI property. Changes affect rendering. |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `SceneNode2D`
- `SceneOrderMode`
- `SceneItems2D`
- `Sprite2D`
- `CollisionWorld2D`
- `MouseEventArgs`
