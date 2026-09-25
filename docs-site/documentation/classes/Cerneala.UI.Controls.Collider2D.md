# Collider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Collider2D.cs`

Provides the nonvisual base for collision shapes owned by a `Sprite2D` in a retained 2D scene.

```csharp
public abstract class Collider2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Collider2D`

## Examples

The collider belongs to the sprite, not to its scene group. Both bindings resolve the same typed data context through the logical tree. The application must declare the `DoorArt` image resource in an enclosing resource scope.

```xml
<RenderSurface2D DataType="Game.DoorState">
    <RenderSurface2D.Scene>
        <Scene2D>
            <Scene2D TranslateX="64" TranslateY="32">
                <Sprite2D Image="$DoorArt" Width="32" Height="32"
                          IsVisible="$DataContext.IsClosed:OneWay">
                    <BoxCollider2D Width="32"
                                   Height="8"
                                   OffsetY="24"
                                   Enabled="$DataContext.IsClosed:OneWay"
                                   CollisionLayer="2"
                                   CollisionMask="4294967295" />
                </Sprite2D>
            </Scene2D>
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

## Remarks

A collider records no drawing commands and has no visual bounds of its own. A live collider can attach only as the single `Sprite2D.Collider`. Markup declares at most one collider directly inside that owner. C# callers assign the typed property, for example `sprite.Collider = new BoxCollider2D { Width = 32, Height = 8 };`. A collider cannot be a scene root, a child of `Scene2D`, a child of another collider, or a visual UI child. Generic `LogicalChildren` and `VisualChildren` mutation cannot bypass this ownership contract, including removal of the owned collider.

An unattached collider may be constructed and configured before assignment; it does not enter a scene's collision world until its owner belongs to that scene. Set its current owner's `Collider` to `null` before transferring it to another valid owner. Assigning an already owned collider throws before replacing the destination owner's existing collider. This rule also applies to triggers. Static [Tile](Cerneala.UI.Controls.Tile.md) placements and imported tile definitions instead own zero or one immutable [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md); their internal adapters cannot be reparented by application code.

`OffsetX` and `OffsetY` move the shape in the collider's local coordinates. The collider's inherited transform channels, the owner's pose, and ancestor scene transforms then produce scene-space geometry. Offsets must be finite. A sprite owner's `X`, `Y`, and `Rotation` move its collider. Sprite draw dimensions, source cropping, `Origin`, `Flip`, and animation frames do not resize, reposition, or mirror collider geometry: the application manages shape dimensions and offsets separately.

`Enabled="false"`, `CollisionLayer="0"`, `IsVisible="false"`, or a non-visible inherited `Visibility` value removes the collider from active collision geometry. `IsTrigger` changes filtering/contact behavior without changing the shape. `CollisionLayer` identifies the collider's bits, while `CollisionMask` identifies the bits it accepts; the default layer is `1` and the default mask accepts every bit.

`IsSimulated="true"` asks the collision world to retain nearby tile collision data around **this collider's own active geometry**, including when it is off camera. It defaults to `false`. It does not start a physics simulation, make collision pairs valid, or apply to descendants, sibling colliders or other colliders owned by the same actor. Mark each needed collider explicitly. The marker contributes interest only while its `Enabled` is true, its `CollisionLayer` is nonzero, and it is effectively visible. A trigger or zero `CollisionMask` still contributes geometric interest; `UIElement.IsEnabled = false` alone does not suppress it. Generated static tile colliders are not automatically marked and do not pin their own terrain. If an actor has not yet been loaded and realized, use an explicit [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md) instead. Marker, shape, transform, filter and visibility changes refresh the interest from current geometry.

Picking is part of the common UI input pipeline, not a collision event system. A collider with `Enabled="true"` and a nonzero `CollisionLayer` can participate in geometric hit testing even when its `CollisionMask` is zero, because the mask filters collision pairs rather than pointer input. `IsHitTestVisible`, UI `IsEnabled`, visibility, and inherited routed events keep their normal `UIElement` meaning. Opacity does not suppress hit testing, and Prism does not alter collision or picking geometry.

The sprite is the routed target for hits on its enabled collider geometry. Its known visual bounds can also participate in picking; attaching a collider does not remove that existing visual-bounds fallback. Handlers use the inherited `MouseDown`, `MouseUp`, move, wheel, focus, keyboard, and text events, which continue through the containing scene groups. No parallel collision-input events are introduced.

All collider properties are UI properties and can be assigned by bindings and Aspect. Motion can interpolate `OffsetX`, `OffsetY`, inherited scene transform channels, `BoxCollider2D.Width`, `BoxCollider2D.Height`, and `CircleCollider2D.Radius`. `Enabled`, `IsTrigger`, `IsSimulated`, `CollisionLayer`, `CollisionMask`, and `PolygonCollider2D.Points` are discrete: use a binding, Aspect declaration, or `@set`, not `@animate`. Unsupported interpolation produces a generator diagnostic instead of an invented mixer.

Because a collider emits no pixels, attaching Prism to it has no collision or visible rendering effect. Apply Prism to the associated visual node or to a debug overlay instead.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `EnabledProperty` | `UiProperty<bool>` | Identifies the `Enabled` UI property. |
| `IsTriggerProperty` | `UiProperty<bool>` | Identifies the `IsTrigger` UI property. |
| `IsSimulatedProperty` | `UiProperty<bool>` | Identifies the `IsSimulated` UI property. |
| `OffsetXProperty` | `UiProperty<float>` | Identifies the local X-offset UI property. |
| `OffsetYProperty` | `UiProperty<float>` | Identifies the local Y-offset UI property. |
| `CollisionLayerProperty` | `UiProperty<uint>` | Identifies the collision-layer UI property. |
| `CollisionMaskProperty` | `UiProperty<uint>` | Identifies the collision-mask UI property. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Enabled` | `bool` | `true` | Gets or sets whether the collider participates in active collision geometry. |
| `IsTrigger` | `bool` | `false` | Gets or sets whether the collider reports contacts without blocking movement. |
| `IsSimulated` | `bool` | `false` | Retains nearby collision data around this collider's active geometry. |
| `OffsetX` | `float` | `0` | Gets or sets the finite local X offset. |
| `OffsetY` | `float` | `0` | Gets or sets the finite local Y offset. |
| `CollisionLayer` | `uint` | `1` | Gets or sets the collider's collision bits. Zero disables active participation. |
| `CollisionMask` | `uint` | `uint.MaxValue` | Gets or sets the collision bits accepted by this collider. |

## Exceptions

Setting `OffsetX` or `OffsetY` to `NaN` or infinity throws `ArgumentOutOfRangeException`.

An invalid parent, a second collider child, or a generic child-collection mutation of an owned collider throws `InvalidOperationException` at runtime or reports `CERNEALAUI005` in markup. Invalid replacement is prevalidated and leaves the current collider installed.

## Applies to

Project: `Cerneala`

## See also

- `BoxCollider2D`
- `CircleCollider2D`
- `PolygonCollider2D`
- `Scene2D`
- `SceneNode2D`
- `MouseEventArgs`
