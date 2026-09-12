# Collider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Collider2D.cs`

Provides the nonvisual base for collision shapes owned by a `Sprite2D` or `TileInstance2D` in a retained 2D scene.

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

A collider records no drawing commands and has no visual bounds of its own. A live collider can attach only through `Sprite2D.Colliders` or `TileInstance2D.Colliders`. Markup declares it directly inside that owner. C# callers use the typed collection, for example `sprite.Colliders.Add(new BoxCollider2D { Width = 32, Height = 8 });`. A collider cannot be a scene root, a child of `Scene2D`, a child of another collider, or a visual UI child. Generic `LogicalChildren` and `VisualChildren` mutation cannot bypass this ownership contract, including removal and reordering of an owned collider.

An unattached collider may be constructed and configured before insertion; it does not enter a scene's collision world until its owner belongs to that scene. Remove it from its current owner's `Colliders` before transferring it to another valid owner. This rule also applies to triggers. Static [Tile](Cerneala.UI.Controls.Tile.md) placements and imported tile definitions instead own immutable [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md) data; their internal adapters cannot be reparented by application code.

`OffsetX` and `OffsetY` move the shape in the collider's local coordinates. The collider's inherited transform channels, the owner's pose, and ancestor scene transforms then produce scene-space geometry. Offsets must be finite. A sprite owner's `X`, `Y`, and `Rotation` move its colliders. Sprite draw dimensions, source cropping, `Origin`, `Flip`, and animation frames do not resize, reposition, or mirror collider geometry: the application manages shape dimensions and offsets separately.

`Enabled="false"`, `CollisionLayer="0"`, `IsVisible="false"`, or a non-visible inherited `Visibility` value removes the collider from active collision geometry. `IsTrigger` changes filtering/contact behavior without changing the shape. `CollisionLayer` identifies the collider's bits, while `CollisionMask` identifies the bits it accepts; the default layer is `1` and the default mask accepts every bit.

Picking is part of the common UI input pipeline, not a collision event system. A collider with `Enabled="true"` and a nonzero `CollisionLayer` can participate in geometric hit testing even when its `CollisionMask` is zero, because the mask filters collision pairs rather than pointer input. `IsHitTestVisible`, UI `IsEnabled`, visibility, and inherited routed events keep their normal `UIElement` meaning. Opacity does not suppress hit testing, and Prism does not alter collision or picking geometry.

The sprite or promoted tile is the routed target for hits on its enabled collider geometry. Its known visual bounds can also participate in picking; attaching a collider does not remove that existing visual-bounds fallback. Handlers use the inherited `MouseDown`, `MouseUp`, move, wheel, focus, keyboard, and text events, which continue through the containing scene groups. No parallel collision-input events are introduced.

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

An invalid parent or a generic child-collection mutation of an owned collider throws `InvalidOperationException`. Invalid markup ownership reports `CERNEALAUI005`.

## Applies to

Project: `Cerneala`

## See also

- `BoxCollider2D`
- `CircleCollider2D`
- `PolygonCollider2D`
- `Scene2D`
- `SceneNode2D`
- `MouseEventArgs`
