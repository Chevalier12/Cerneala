# Scene2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2D.cs`

Groups retained 2D scene nodes in deterministic drawing order.

```csharp
[ContentProperty(nameof(Children))]
public class Scene2D : SceneNode2D
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

### Reusable scene components

Pair `HouseView.crn` with `HouseView.crn.cs` to define a reusable scene group.
The markup root initializes the component itself, not an extra nested group.

`HouseView.crn`:

```xml
<Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
    <Scene2D.Resources>
        <resources:ImageResource Name="HouseArt" Source="Assets/house.png" />
    </Scene2D.Resources>
    <Sprite2D Image="$HouseArt" Width="32" Height="24" />
    <Sprite2D Name="Door" X="4" Y="16" Width="8" Height="8" />
</Scene2D>
```

`HouseView.crn.cs`:

```csharp
using Cerneala.UI.Controls;

namespace Game;

public partial class HouseView : Scene2D
{
}
```

Import its CLR namespace in the consuming document:

```xml
<RenderSurface2D xmlns:local="clr-namespace:Game" RedrawMode="OnDemand">
    <RenderSurface2D.Scene>
        <Scene2D OrderMode="Layer">
            <local:HouseView TranslateX="30" TranslateY="20" Scale="2" Layer="10" />
            <local:HouseView TranslateX="90" TranslateY="40" Layer="20" />
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

A component can also be the single direct child of `RenderSurface2D.Scene`.
Position and transform channels affect the entire instance; `Layer` is interpreted
by its containing scene's `OrderMode`, just like an ordinary `Scene2D` group.

## Remarks

### Component construction and scope

The paired class must match the file name, derive from `Scene2D`, and be a
concrete, non-nested, non-generic, non-file-local `partial` class. Do not declare
instance constructors: the generator supplies a public parameterless constructor
that initializes the instance's properties, resources, handlers, and logical
children. A paired root cannot declare `Name`; it represents `this`.

Named descendants become private generated members in the paired class. They
must not conflict with members declared in code-behind. Each new component has
its own child instances, local resource declarations, and name scope. `$root`
inside its markup refers to that component, including custom UI properties
declared in its companion class. To type `$DataContext` bindings, declare a root
`DataType`. Generated `$DataContext` bindings resolve the nearest context through
the logical ancestors and rebind when that context changes. This lookup does not
require copying the ancestor's `DataContext` value onto every logical node.

The component uses the existing scene attachment, binding, Aspect, Motion, Prism,
template, collision, and invalidation paths. Detaching and reattaching does not
rerun its constructor or rebuild its permanent children. Deriving from `Scene2D`
does not expose a new custom recording API; applications compose the provided
scene nodes. A paired document emits the partial component, not a standalone
`HouseViewFactory`. An unpaired `.crn` file retains the ordinary factory behavior.

### Scene behavior

Direct markup children are added to `Children`. `OrderMode="Source"`, the default, records them from first to last, so later children draw after earlier children. Adding, replacing, removing, or clearing children invalidates the owning surface.

`OrderMode="Layer"` sorts by each child's `Layer` value. `OrderMode="LayerThenY"` sorts first by `Layer`, then by the bottom edge of the child's transformed scene-space bounds. Smaller values draw first. Both modes are stable: equal keys retain source collection order, and neither mode mutates `Children`. A missing or unknown bound uses a Y anchor of `0` rather than removing the node.

Picking uses that same effective order in reverse, so the last visible eligible node at a point wins. The chosen node enters the ordinary UI route through its scene ancestors and the owning `RenderSurface2D`; scene groups do not create a second router or event family.

Children belong to the logical tree and inherit data context and attachment state. Setting either `IsVisible` to `false` or `Visibility` to a non-visible value skips the group and all of its descendants.

The root group owns a `CollisionWorld2D`. `CollisionWorld` on any nested group resolves to the same root-owned world. Structural and collider-property mutations update that world incrementally; removing a subtree removes its indexed colliders before the next query.

The scene owns the query world, not the collision shapes themselves. Live colliders belong only to a `Sprite2D` or `TileInstance2D` through its `Colliders` collection. `Scene2D.Children` rejects collider nodes, including in derived markup components. Static tile collision geometry belongs to `Tile.Colliders` or `TileDefinition2D.Colliders` as immutable descriptors.

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
