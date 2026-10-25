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

A collider records no drawing commands and has no visual bounds of its own. A live collider can attach only as the single `Sprite2D.Collider`. Markup declares at most one collider directly inside that owner. C# callers assign the typed property, for example `sprite.Collider = new BoxCollider2D { Width = 32, Height = 8 };`. A collider cannot be a scene root, a child of `Scene2D`, a child of another collider, or a visual UI child. Generic `LogicalChildren` and `VisualChildren` mutation cannot bypass this ownership contract, including removal of the owned collider.

An unattached collider may be constructed and configured before assignment; it does not enter a scene's collision world until its owner belongs to that scene. Set its current owner's `Collider` to `null` before transferring it to another valid owner. Assigning an already owned collider throws before replacing the destination owner's existing collider. This rule also applies to triggers. Static [Tile](Cerneala.UI.Controls.Tile.md) placements and imported tile definitions instead own zero or one immutable [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md); their internal adapters cannot be reparented by application code.

`OffsetX` and `OffsetY` move the shape in the collider's local coordinates. The collider's inherited transform channels, the owner's pose, and ancestor scene transforms then produce scene-space geometry. Offsets must be finite. A sprite owner's `X`, `Y`, and `Rotation` move its collider. Sprite draw dimensions, source cropping, `Origin`, `Flip`, and animation frames do not resize, reposition, or mirror collider geometry: the application manages shape dimensions and offsets separately.

`Enabled="false"`, `CollisionLayer="0"`, `IsVisible="false"`, or a non-visible inherited `Visibility` value removes the collider from active collision geometry. `IsTrigger` changes filtering/contact behavior without changing the shape; the `Entered` and `Exited` contact events treat triggers and solid colliders alike. `CollisionLayer` identifies the collider's bits, while `CollisionMask` identifies the bits it accepts; the default layer is `1` and the default mask accepts every bit.

`IsSimulated="true"` asks the collision world to retain nearby tile collision data around **this collider's own active geometry**, including when it is off camera. It defaults to `false`. It does not start a physics simulation, make collision pairs valid, or apply to descendants, sibling colliders or other colliders owned by the same actor. Mark each needed collider explicitly. The marker contributes interest only while its `Enabled` is true, its `CollisionLayer` is nonzero, and it is effectively visible. A trigger or zero `CollisionMask` still contributes geometric interest; `UIElement.IsEnabled = false` alone does not suppress it. Generated static tile colliders are not automatically marked and do not pin their own terrain. If an actor has not yet been loaded and realized, use an explicit [SceneCollisionRegion2D](Cerneala.UI.Controls.SceneCollisionRegion2D.md) instead. Marker, shape, transform, filter and visibility changes refresh the interest from current geometry.

Picking is part of the common UI input pipeline and is separate from the `Entered` and `Exited` contact events. A collider with `Enabled="true"` and a nonzero `CollisionLayer` can participate in geometric hit testing even when its `CollisionMask` is zero, because the mask filters collision pairs rather than pointer input. `IsHitTestVisible`, UI `IsEnabled`, visibility, and inherited routed events keep their normal `UIElement` meaning. Opacity does not suppress hit testing, and Prism does not alter collision or picking geometry.

The sprite is the routed target for hits on its enabled collider geometry. Its known visual bounds can also participate in picking; attaching a collider does not remove that existing visual-bounds fallback. Handlers use the inherited `MouseDown`, `MouseUp`, move, wheel, focus, keyboard, and text events, which continue through the containing scene groups. Colliders have no separate pointer or keyboard events.

All collider properties are UI properties and can be assigned by bindings and Aspect. Motion can interpolate `OffsetX`, `OffsetY`, inherited scene transform channels, `BoxCollider2D.Width`, `BoxCollider2D.Height`, and `CircleCollider2D.Radius`. `Enabled`, `IsTrigger`, `IsSimulated`, `CollisionLayer`, `CollisionMask`, and `PolygonCollider2D.Points` are discrete: use a binding, Aspect declaration, or `@set`, not `@animate`. Unsupported interpolation produces a generator diagnostic instead of an invented mixer.

Because a collider emits no pixels, attaching Prism to it has no collision or visible rendering effect. Apply Prism to the associated visual node or to a debug overlay instead.

### Contact events

`Entered` occurs when another collider starts touching this collider. `Exited` occurs when a contact that `Entered` reported ends. The sender is the collider that receives the event, and `ColliderContactEventArgs.Other` is the collider on the other side. Each side receives its own events: when the coin and the fox in the example both have handlers, the coin receives `Entered` with `Other` set to the fox's collider, and the fox receives `Entered` with `Other` set to the coin's collider. A collider without handlers receives nothing, but it can still be the `Other` of a collider that has handlers.

Two colliders touch when `CollisionWorld2D.Intersects` returns `true` for them: both are active in the same root scene's collision world, each `CollisionMask` accepts the other collider's `CollisionLayer`, and the shapes overlap or meet within the edge-contact epsilon of `1e-5` scene units. Triggers and solid colliders follow the same rule.

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

An invalid parent, a second collider child, or a generic child-collection mutation of an owned collider throws `InvalidOperationException` at runtime or reports `CERNEALAUI005` in markup. Invalid replacement is prevalidated and leaves the current collider installed.

Adding or removing an `Entered` or `Exited` handler on a thread other than the owner thread of the collider's UI root or simulation context throws `InvalidOperationException`. Exceptions thrown by contact handlers reach the caller of the delivering update as one `AggregateException`, after all events of that delivery are raised.

## Applies to

Project: `Cerneala`

## See also

- `BoxCollider2D`
- `CircleCollider2D`
- `PolygonCollider2D`
- `ColliderContactEventArgs`
- `CollisionWorld2D`
- `Scene2D`
- `SceneNode2D`
- `SceneSimulationContext2D`
- `MouseEventArgs`
