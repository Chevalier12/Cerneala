# Collider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Collider2D.cs`

Provides the nonvisual base for collision shapes owned by a retained 2D scene.

```csharp
public abstract class Collider2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Collider2D`

## Examples

The sprite and colliders below share a parent and inherit the same data context. No separate entity identifier or wiring object is required.

```xml
<RenderSurface2D DataType="Game.DoorState">
    <RenderSurface2D.Scene>
        <Scene2D>
            <Scene2D TranslateX="64" TranslateY="32">
                <Sprite2D IsVisible="$DataContext.IsClosed:OneWay" />
                <BoxCollider2D Width="32"
                               Height="8"
                               OffsetY="24"
                               Enabled="$DataContext.IsClosed:OneWay"
                               CollisionLayer="2"
                               CollisionMask="4294967295" />
            </Scene2D>
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

## Remarks

A collider records no drawing commands and has no visual bounds of its own. Put it beside the visual nodes that represent the same entity under a common `Scene2D`; inherited data context and group transforms then keep the visual and collision geometry together.

`OffsetX` and `OffsetY` move the shape in the collider's local coordinates. The inherited scene transform channels are then composed with all ancestor `Scene2D` transforms to produce scene-space geometry. Offsets must be finite.

`Enabled="false"`, `CollisionLayer="0"`, `IsVisible="false"`, or a non-visible inherited `Visibility` value removes the collider from active collision geometry. `IsTrigger` changes filtering/contact behavior without changing the shape. `CollisionLayer` identifies the collider's bits, while `CollisionMask` identifies the bits it accepts; the default layer is `1` and the default mask accepts every bit.

Picking is part of the common UI input pipeline, not a collision event system. A collider with `Enabled="true"` and a nonzero `CollisionLayer` can participate in geometric hit testing even when its `CollisionMask` is zero, because the mask filters collision pairs rather than pointer input. `IsHitTestVisible`, UI `IsEnabled`, visibility, and inherited routed events keep their normal `UIElement` meaning. Opacity does not suppress hit testing, and Prism does not alter collision or picking geometry.

When a non-collider scene entity has one or more direct collider children, their enabled geometry forms that entity's picking region and the entity is the routed target. A collider declared directly under the scene root can itself be the target. Handlers use the inherited `MouseDown`, `MouseUp`, move, wheel, focus, keyboard, and text events; no parallel collision-input events are introduced.

All collider properties are UI properties and can be assigned by bindings and Aspect. Motion can interpolate `OffsetX`, `OffsetY`, inherited scene transform channels, `BoxCollider2D.Width`, `BoxCollider2D.Height`, and `CircleCollider2D.Radius`. `Enabled`, `IsTrigger`, `CollisionLayer`, `CollisionMask`, and `PolygonCollider2D.Points` are discrete: use a binding, Aspect declaration, or `@set`, not `@animate`. Unsupported interpolation produces a generator diagnostic instead of an invented mixer.

Because a collider emits no pixels, attaching Prism to it has no collision or visible rendering effect. Apply Prism to the associated visual node or to a debug overlay instead.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `EnabledProperty` | `UiProperty<bool>` | Identifies the `Enabled` UI property. |
| `IsTriggerProperty` | `UiProperty<bool>` | Identifies the `IsTrigger` UI property. |
| `OffsetXProperty` | `UiProperty<float>` | Identifies the local X-offset UI property. |
| `OffsetYProperty` | `UiProperty<float>` | Identifies the local Y-offset UI property. |
| `CollisionLayerProperty` | `UiProperty<uint>` | Identifies the collision-layer UI property. |
| `CollisionMaskProperty` | `UiProperty<uint>` | Identifies the collision-mask UI property. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `true` | Gets or sets whether the collider participates in active collision geometry. |
| `IsTrigger` | `bool` | `false` | Gets or sets whether the collider reports contacts without blocking movement. |
| `OffsetX` | `float` | `0` | Gets or sets the finite local X offset. |
| `OffsetY` | `float` | `0` | Gets or sets the finite local Y offset. |
| `CollisionLayer` | `uint` | `1` | Gets or sets the collider's collision bits. Zero disables active participation. |
| `CollisionMask` | `uint` | `uint.MaxValue` | Gets or sets the collision bits accepted by this collider. |

## Exceptions

Setting `OffsetX` or `OffsetY` to `NaN` or infinity throws `ArgumentOutOfRangeException`.

## Applies to

Project: `Cerneala`

## See also

- `BoxCollider2D`
- `CircleCollider2D`
- `PolygonCollider2D`
- `Scene2D`
- `SceneNode2D`
- `MouseEventArgs`
