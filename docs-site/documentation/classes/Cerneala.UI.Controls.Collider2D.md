# Collider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Collider2D.cs`

Provides the nonvisual base for collision shapes owned by a `Sprite2D`, or by a `Scene2D` as its `Vision`, in a retained 2D scene.

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

A trigger can react when another collider touches it. This independent scene delivers its contact events from `SceneSimulationContext2D.Update()`; a scene shown by an attached `RenderSurface2D` receives the same events from `UiHost.Update`.

```csharp
using Cerneala.UI.Controls;

BoxCollider2D coinBox = new() { Width = 10, Height = 10, IsTrigger = true };
BoxCollider2D foxBox = new() { Width = 10, Height = 10 };
Sprite2D coin = new() { Collider = coinBox };
Sprite2D fox = new() { X = 100, Collider = foxBox };

Scene2D scene = new();
scene.Children.Add(coin);
scene.Children.Add(fox);
using SceneSimulationContext2D simulation = new(scene);

coinBox.Entered += (_, e) =>
{
    if (e.Other == foxBox)
    {
        scene.Children.Remove(coin); // the coin disappears when the fox touches it
    }
};

fox.X = 5;           // the two 10 x 10 boxes now overlap
simulation.Update(); // raises coinBox.Entered once, with e.Other == foxBox
```

## Remarks

A collider records no drawing commands and has no visual bounds of its own. A live collider can attach in only two ways:

| Owner and property | Accepts |
| --- | --- |
| `Sprite2D.Collider` | Any live collider except a `ConeCollider2D`. This is the sprite's body. |
| `Sprite2D.Vision` or `Scene2D.Vision` | A `ConeCollider2D` only. |

Markup declares the body as at most one collider directly inside a sprite. It declares a cone in the `<Sprite2D.Vision>` or `<Scene2D.Vision>` property element, exactly one cone in each; a cone there is not the sprite's "one collider". A `ConeCollider2D` directly inside a sprite, or anywhere else that is not a `Vision`, is a `CERNEALAUI005` error that says it can only be a `Vision`; see [ConeCollider2D, Markup usage](Cerneala.UI.Controls.ConeCollider2D.md#markup-usage). C# callers assign the typed property, for example `sprite.Collider = new BoxCollider2D { Width = 32, Height = 8 };` or `sprite.Vision = new ConeCollider2D { Radius = 150, Angle = 90 };`. A collider cannot be a scene root, one of the `Children` of a `Scene2D`, a child of another collider, or a visual UI child. A `Scene2D` accepts a collider only through its `Vision` property. Generic `LogicalChildren` and `VisualChildren` mutation cannot bypass this ownership contract, including removal of the owned collider.

An unattached collider may be constructed and configured before assignment; it does not enter a scene's collision world until its owner belongs to that scene. Set its current owner's `Collider` or `Vision` to `null` before transferring it to another valid owner. Assigning an already owned collider throws before replacing the destination owner's existing collider. This rule also applies to triggers. Assigning a `ConeCollider2D` to `Sprite2D.Collider` throws `InvalidOperationException` with a message that names `Vision`, and the body already in `Collider` stays attached. Static [Tile](Cerneala.UI.Controls.Tile.md) placements and imported tile definitions instead own zero or one immutable [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md); their internal adapters cannot be reparented by application code.

`OffsetX` and `OffsetY` move the shape in the collider's local coordinates. The collider's inherited transform channels, the owner's pose, and ancestor scene transforms then produce scene-space geometry. Offsets must be finite. A sprite owner's `X`, `Y`, and `Rotation` move its collider. Sprite draw dimensions, source cropping, `Origin`, `Flip`, and animation frames do not resize, reposition, or mirror collider geometry: the application manages shape dimensions and offsets separately. The one exception is a `Sprite2D.Vision` cone, which follows the image of its sprite: the flip that the image is drawn with (the sprite's `Flip` composed with the `Flip` of the current animation frame) mirrors the cone's `Direction` and its apex, as described in [ConeCollider2D](Cerneala.UI.Controls.ConeCollider2D.md).

`Enabled="false"`, `CollisionLayer="0"`, `IsVisible="false"`, or a non-visible inherited `Visibility` value removes the collider from active collision geometry. `IsTrigger` changes filtering/contact behavior without changing the shape; the `Entered` and `Exited` contact events treat triggers and solid colliders alike. `IsTrigger` changes nothing on a `Vision` cone, which is a sensor either way. `CollisionLayer` identifies the collider's bits, while `CollisionMask` identifies the bits it accepts; the default layer is `1` and the default mask accepts every bit.

`IsSimulated="true"` asks the collision world to retain nearby tile collision data around **this collider's own active geometry**, including when it is off camera. It defaults to `false`. It does not start a physics simulation, make collision pairs valid, or apply to descendants, sibling colliders or other colliders owned by the same actor. Mark each needed collider explicitly. The marker contributes interest only while its `Enabled` is true, its `CollisionLayer` is nonzero, and it is effectively visible. A trigger or zero `CollisionMask` still contributes geometric interest; `UIElement.IsEnabled = false` alone does not suppress it. Generated static tile colliders are not automatically marked and do not pin their own terrain. If an actor has not yet been loaded and realized, use an explicit [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md) instead. Marker, shape, transform, filter and visibility changes refresh the interest from current geometry.

Picking is part of the common UI input pipeline and is separate from the `Entered` and `Exited` contact events. A collider with `Enabled="true"` and a nonzero `CollisionLayer` can participate in geometric hit testing even when its `CollisionMask` is zero, because the mask filters collision pairs rather than pointer input. `IsHitTestVisible`, UI `IsEnabled`, visibility, and inherited routed events keep their normal `UIElement` meaning. Opacity does not suppress hit testing, and Prism does not alter collision or picking geometry.

A `Vision` cone is never picked: a click inside the cone, but outside the sprite's body and visual bounds, does not reach the sprite, and the cone never becomes the target of an input event.

The sprite is the routed target for hits on its enabled collider geometry. Its known visual bounds can also participate in picking; attaching a collider does not remove that existing visual-bounds fallback. Handlers use the inherited `MouseDown`, `MouseUp`, move, wheel, focus, keyboard, and text events, which continue through the containing scene groups. Colliders have no separate pointer or keyboard events.

All collider properties are UI properties and can be assigned by bindings and Aspect. Motion can interpolate `OffsetX`, `OffsetY`, inherited scene transform channels, `BoxCollider2D.Width`, `BoxCollider2D.Height`, `CircleCollider2D.Radius`, and `ConeCollider2D.Radius`, `ConeCollider2D.Angle` and `ConeCollider2D.Direction` (see [ConeCollider2D, Motion](Cerneala.UI.Controls.ConeCollider2D.md#motion)). `Enabled`, `IsTrigger`, `IsSimulated`, `CollisionLayer`, `CollisionMask`, and `PolygonCollider2D.Points` are discrete: use a binding, Aspect declaration, or `@set`, not `@animate`. Unsupported interpolation produces a generator diagnostic instead of an invented mixer.

Because a collider emits no pixels, attaching Prism to it has no collision or visible rendering effect. Apply Prism to the associated visual node or to a debug overlay instead.

### Contact events

`Entered` occurs when another collider starts touching this collider. `Exited` occurs when a contact that `Entered` reported ends. The sender is the collider that receives the event, and `ColliderContactEventArgs.Other` is the collider on the other side. Each side receives its own events: when the coin and the fox in the example both have handlers, the coin receives `Entered` with `Other` set to the fox's collider, and the fox receives `Entered` with `Other` set to the coin's collider. A collider without handlers receives nothing, but it can still be the `Other` of a collider that has handlers.

Two colliders touch when `CollisionWorld2D.Intersects` returns `true` for them: both are active in the same root scene's collision world, each `CollisionMask` accepts the other collider's `CollisionLayer`, and the shapes overlap or meet within the edge-contact epsilon of `1e-5` scene units. Triggers and solid colliders follow the same rule.

A [Vision](Cerneala.UI.Controls.ConeCollider2D.md#a-vision-is-a-sensor) cone takes part in contact events as a sensor, and only the sensor reports. When a body touches a cone, the cone raises `Entered` and `Exited`, with the cone as the sender and the body as `Other`. The body receives no event for the cone, even when it has handlers, so the `Other` of an event is never a `Vision`. Two cones never touch, and a cone never touches a collider inside its owner's subtree, so these cases raise no event either. For example, when a fox walks into the cone of a bee, `bee.Vision.Entered` is raised with `Other` set to `fox.Collider`, and `fox.Collider.Entered` is not raised.

`Exited` also occurs when a contact ends without movement:

- `Enabled` becomes `false`, `CollisionLayer` becomes `0`, or a layer or mask change rejects the pair;
- the collider, its owner, or an ancestor scene group stops being visible;
- the owner or an ancestor scene group is removed from the scene;
- the owner's `Sprite2D.Collider` is replaced or set to `null`;
- one of the two colliders moves to another root scene.

If both colliders move together into another root scene that has a simulation context, the contact continues there and no event is raised.

A setter that changes geometry never raises `Entered` or `Exited` itself. The world collects the changes, and each update delivers them at one fixed point:

- a scene shown by an attached `RenderSurface2D` receives its events during `UiHost.Update`, after input dispatch (including `InputHeld`) and before input processing;
- a scene owned by an independent `SceneSimulationContext2D` receives its events during `SceneSimulationContext2D.Update()`, after the relay drain and the refresh of scene interests.

A scene without a simulation context delivers no contact events. After a context attaches, its next delivery evaluates the colliders that have handlers.

In one delivery, every `Exited` comes before every `Entered`. Inside each group, events follow the receiver's position in the collision world's attachment order, then the position of `Other`. This is the stable order that `CollisionWorld2D.Overlap` uses; a collider that is no longer in the world keeps the position of its most recent attachment. When one `UiHost.Update` delivers several scenes, each group is ordered by scene first, in the order in which each surface attached its current scene.

A handler can move, remove, or reconfigure colliders. The world evaluates those changes at the next delivery, not inside the current one. For example, a handler that moves the fox away from the coin produces the coin's `Exited` during the next update.

While a scene shown by an attached `RenderSurface2D` has collider changes or new handlers waiting for the next delivery, a `Window` renders the next frame by itself, even when the surface uses `RenderSurface2DRedrawMode.OnDemand` and nothing else asks for a frame. So the coin's `Exited` arrives without other input. Servo `WaitForIdleAsync` does not report idle until that delivery. An application that drives `UiHost` itself receives the events at its next `UiHost.Update`.

Adding a handler raises nothing at once. If the collider already touches another collider, the next delivery raises `Entered` for that contact. Adding another handler to a collider that already reported a contact does not report that contact again. When the last handler is removed, so that the collider has neither an `Entered` nor an `Exited` handler, the collider forgets its reported contacts without raising `Exited`; a later first handler receives `Entered` again for contacts that still exist. Add and remove handlers on the thread that owns the collider's UI root or simulation context.

When a handler throws, the handlers after it on the same event do not run, as with any .NET multicast delegate, but the delivery still raises its remaining events. After the last event, the update rethrows the handler exceptions together as one `AggregateException`. The contact state was already updated, so the failed event is not raised again.

A scene in which no collider has a contact handler does no contact work when colliders change: nothing is recorded and no extra exact shape test runs. With handlers, a delivery tests each changed collider that has handlers against the nearby colliders, each changed collider without handlers only against nearby colliders that have handlers, and both against their already reported contacts. This work counts in `CollisionWorld2DDiagnosticsSnapshot.BroadphaseCandidateCount` and `ExactTestCount`, not in `QueryCount`.

When the other side is a static tile, `Other` is the map's internal tile collider: the same `Collider2D` object that `CollisionWorld2D.Overlap` reports in `CollisionHit2D.Collider` for that tile. That object belongs to the `TileMap2D`; add handlers to sprite colliders, not to tile colliders.

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
| `IsTrigger` | `bool` | `false` | Gets or sets whether the collider is a trigger. Triggers never block `MoveAndCollide`. |
| `IsSimulated` | `bool` | `false` | Retains nearby collision data around this collider's active geometry. |
| `OffsetX` | `float` | `0` | Gets or sets the finite local X offset. |
| `OffsetY` | `float` | `0` | Gets or sets the finite local Y offset. |
| `CollisionLayer` | `uint` | `1` | Gets or sets the collider's collision bits. Zero disables active participation. |
| `CollisionMask` | `uint` | `uint.MaxValue` | Gets or sets the collision bits accepted by this collider. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `Entered` | `EventHandler<ColliderContactEventArgs>?` | Occurs at a contact delivery after another collider starts touching this collider. |
| `Exited` | `EventHandler<ColliderContactEventArgs>?` | Occurs at a contact delivery after a contact that `Entered` reported ends. |

## Exceptions

Setting `OffsetX` or `OffsetY` to `NaN` or infinity throws `ArgumentOutOfRangeException`.

An invalid parent, a second collider child, or a generic child-collection mutation of an owned collider throws `InvalidOperationException` at runtime or reports `CERNEALAUI005` in markup. Assigning a `ConeCollider2D` to `Sprite2D.Collider`, or a collider that already has an owner to any `Collider` or `Vision`, also throws `InvalidOperationException`. Invalid replacement is prevalidated and leaves the current collider installed. In markup, a `ConeCollider2D` outside `Sprite2D.Vision` and `Scene2D.Vision` reports `CERNEALAUI005` with a message that names `Vision`, and so does any element other than a cone inside those property elements.

Adding or removing an `Entered` or `Exited` handler on a thread other than the owner thread of the collider's UI root or simulation context throws `InvalidOperationException`. Exceptions thrown by contact handlers reach the caller of the delivering update as one `AggregateException`, after all events of that delivery are raised.

## Applies to

Project: `Cerneala`

## See also

- `BoxCollider2D`
- `CircleCollider2D`
- `PolygonCollider2D`
- `ConeCollider2D`
- `ColliderContactEventArgs`
- `CollisionWorld2D`
- `Scene2D`
- `SceneNode2D`
- `SceneSimulationContext2D`
- `MouseEventArgs`
