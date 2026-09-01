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
    <Scene2D>
        <Sprite2D Source="$DataContext.Background:OneWay"
                  Destination="$DataContext.BoardBounds:OneWay" />
        <Sprite2D Source="$DataContext.Player:OneWay"
                  Destination="$DataContext.PlayerBounds:OneWay" />
    </Scene2D>
</RenderSurface2D.Scene>
```

## Remarks

Direct markup children are added to `Children`. Nodes are recorded from first to last, so later children draw after earlier children. Adding, replacing, removing, or clearing children invalidates the owning surface.

Children belong to the logical tree and inherit data context and attachment state. Setting either `IsVisible` to `false` or `Visibility` to a non-visible value skips the group and all of its descendants.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2D()` | Creates an empty owned child collection. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `Collection<SceneNode2D>` | Ordered retained nodes recorded by the group. |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `SceneNode2D`
- `SceneItems2D`
- `Sprite2D`
