# ConeCollider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ConeCollider2D.cs`

Defines a cone-shaped collision area in local scene coordinates: a slice of a circle whose point, the apex, is the collider origin.

```csharp
public sealed class ConeCollider2D : Collider2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Collider2D` -> `ConeCollider2D`

## Examples

Three cones. `Direction` is in degrees: 0 looks right, and the angle grows clockwise on screen, so 90 looks down.

```csharp
using Cerneala.UI.Controls;

// A guard: narrow, but far. It looks left.
ConeCollider2D guard = new() { Radius = 300, Angle = 30, Direction = 180 };

// A bee: wide, but near. It looks down, and `OffsetX = 12` moves its apex 12 units along local +X.
ConeCollider2D bee = new() { Radius = 150, Angle = 120, Direction = 90, OffsetX = 12 };

// An owl: sees all around. Angle 360 is a full circle.
ConeCollider2D owl = new() { Radius = 200, Angle = 360 };

guard.Direction = 450; // accepted; guard.Direction still reads 450, and the cone looks down like 90
```

A cone belongs to an owner as its `Vision`. A sprite can have a body and a vision at the same time, and a scene group can have a vision of its own.

```csharp
using Cerneala.UI.Controls;

Sprite2D bee = new()
{
    Collider = new BoxCollider2D { Width = 10, Height = 10, OffsetX = -12, OffsetY = -5 }
};
bee.Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 };
// bee.Collider is the box. bee.Vision is the cone. Both are logical children of bee.

Scene2D dragon = new() { X = 100, Y = 50 };
dragon.Vision = new ConeCollider2D { Radius = 300, Angle = 30, Direction = 180 }; // the whole group looks left

Sprite2D wrong = new();
wrong.Collider = new ConeCollider2D(); // throws InvalidOperationException; the message says to use Sprite2D.Vision or Scene2D.Vision
```

## Remarks

The apex is the collider origin, moved by `OffsetX` and `OffsetY` (see [Collider2D](Cerneala.UI.Controls.Collider2D.md)). The cone reaches `Radius` units from the apex. The cone is a slice of a circle: two straight sides from the apex, joined by an arc. The cone is symmetric around `Direction`: it covers from `Direction - Angle / 2` to `Direction + Angle / 2`.

`Direction` and `Angle` are in degrees. `Direction` 0 looks along +X (right). Angles grow clockwise on screen, because +Y points down.

| `Direction` | The cone looks |
| --- | --- |
| `0` | right |
| `90` | down |
| `180` | left |
| `-90` | up |

`Direction` accepts any finite value. The property keeps the value as set, and only the shape uses it normalized: after `cone.Direction = 450`, `cone.Direction` reads `450` and the cone looks down, exactly like `90`. The same holds for `-720` (like `0`) and `1e6` (like `280`).

`Angle` must be greater than 0 and at most 360. A cone with `Angle` 360 is a full circle: it gives the same results as a [CircleCollider2D](Cerneala.UI.Controls.CircleCollider2D.md) with the same radius. The part of the circle that the cone leaves out is a notch behind it. A shape that lies only inside the notch does not touch the cone. A long box that crosses the notch, with both ends outside it, touches the cone.

| For | Values |
| --- | --- |
| Guard: narrow, but far | `Radius = 300`, `Angle = 30` |
| Bee: wide, but near | `Radius = 150`, `Angle = 120` |
| Owl: sees all around | `Radius = 200`, `Angle = 360` |
| Pac-Man: everything except the back | `Radius = 100`, `Angle = 300` |

A shape touches the cone when it overlaps the cone or meets its edge within the same edge-contact epsilon, `1e-5` scene units, that other colliders use. For example, with `Direction = 45` and `Angle = 90` the sides lie on the +X and +Y axes. A box whose edge ends `0.000005` units away from the +X side touches the cone. A box `0.00002` units away does not.

The collider's own `Rotation`, in radians, adds to `Direction`: a cone with `Direction = 90` and a `Rotation` of `MathF.PI / 2` (a quarter turn) looks left. The transforms of the owner and of the ancestor scene nodes then move and rotate the cone, as for every other collider.

When the owner is a `Sprite2D`, the cone follows the image that the sprite draws. It uses the flip that the user sees: the sprite's `Flip` composed with the `Flip` of the current animation frame, exclusive-or on each axis. A horizontal `Flip` on the sprite and a horizontal `Flip` on the frame cancel out: the image is drawn unmirrored, and the cone is not mirrored either. That flip mirrors two things, `Direction` and the apex. This differs from the other colliders, whose geometry ignores `Flip` and animation frames. A cone owned by a `Scene2D` is never flipped, because `Scene2D` has no `Flip` property.

`Direction` becomes:

| Flip | `Direction` becomes | Example: `Direction = 30` |
| --- | --- | --- |
| `None` | `Direction` | `30` |
| `Horizontal` | `180 - Direction` | `150` |
| `Vertical` | `-Direction` | `-30` |
| `Horizontal` and `Vertical` | `Direction + 180` | `210` |

The apex mirrors across the centre line of the sprite's local image rectangle. A flipped image is drawn by swapping its texture coordinates inside a destination rectangle that does not move, so that rectangle is the mirror. A horizontal flip mirrors across the vertical centre line, `x = rectangle.X + rectangle.Width / 2`, so the apex's `OffsetX` becomes `2 * centreX - OffsetX`. A vertical flip does the same with `OffsetY` and the horizontal centre line.

For example, take a 32 by 32 image at `Origin = (0, 0)`. It fills x 0 to 32 and y 0 to 32 from the sprite anchor, so the centre lines are `x = 16` and `y = 16`. The cone is `Radius = 50`, `Angle = 90`, `Direction = 0`, `OffsetX = 28`, `OffsetY = 10`:

| Flip | Apex (x, y) | The cone looks |
| --- | --- | --- |
| `None` | `(28, 10)` | right |
| `Horizontal` | `(4, 10)` | left |
| `Vertical` | `(28, 22)` | right |
| `Horizontal` and `Vertical` | `(4, 22)` | left |

With `Flip = Horizontal`, a fox box that spans x -40 to -30 at the height of the apex is 34 to 44 units from the apex at x 4, so the cone sees it. From the unmirrored apex at x 28 the same box would be 58 to 68 units away, out of reach. With `Origin = (16, 16)` the image fills x -16 to 16, so the centre line passes through the anchor: `OffsetX = 0` stays at 0, and `OffsetX = 6` becomes -6.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

Sprite2D bee = new()
{
    Image = new ImageReference(beeImage),   // a 32 by 32 IDrawImage
    Origin = new DrawPoint(0, 0),
    Vision = new ConeCollider2D { Radius = 50, Angle = 90, Direction = 0, OffsetX = 28, OffsetY = 10 }
};

bee.Flip = RenderSurface2DSpriteFlip.Horizontal;
// The apex is now at local (4, 10) and the cone looks left.
```

Which rectangle is the mirror:

- With an image, it is the rectangle the sprite is drawn in: `Width` and `Height` when they are set, otherwise the size of the image or of the current frame. `Origin` places its top-left corner at `-Origin`, scaled by the drawn size over the image size. With `Width = 64` and `Height = 64` on the 32 by 32 image above, the centre line is `x = 32`, and `OffsetX = 28` becomes 36.
- For a trimmed animation frame it is the logical, untrimmed frame, because the visible pixels are mirrored inside it.
- With no image, a declared `Width` and `Height` give the rectangle when `Origin` is zero. For `Width = 20`, `Height = 10` and `Origin = (0, 0)`, the centre is `(10, 5)`.
- When the sprite has no known rectangle, the axis is the sprite anchor, and `OffsetX` becomes `-OffsetX` and `OffsetY` becomes `-OffsetY`. The rectangle is unknown when there is no image and no declared `Width` and `Height`, and also when `Origin` is not zero and no size is known to place it. An image that has not finished loading counts as no image.

The offsets are mirrored as you wrote them, before the cone's own transform channels (`Rotation`, `Scale`, `TranslateX`, and the others) and the transforms of its owners apply.

The collision world reads the cone again, and contacts change at the next update, when the flip or the centre of the mirror changes. That happens when any of these changes: the sprite's `Flip`, `Image`, `Width`, `Height`, `Origin`, `SourceX`, `SourceY`, `SourceWidth`, `SourceHeight`, `Animations` or `AnimationState`; when an animation advances to a frame that changes the composed flip or the centre; and when `RestartAnimation()` does. The sprite compares the mirror with the one the cone last used, so a change that leaves it the same does nothing. For example, an animation that goes from one unflipped frame to another unflipped frame does not touch the cone, and neither does a new `Width` on a sprite whose `Flip` is `None`. A sprite without a `Vision` does none of this work. A sprite with a `Vision` keeps following its frames while it is culled out of view, because the cone lives in the collision world whether or not the sprite is drawn.

One case is not followed. When the image of a resource finishes loading after the cone was read, the sprite does not tell the collision world, because recording a frame must not change collision state. The apex keeps the centre it had, the anchor if no rectangle was known, until the sprite's next change in the collision world, such as a new `X`, `Y`, `Rotation` or one of the properties above. For example, a sprite with `Flip = Horizontal` and `OffsetX = 28` has its apex at `-28` while its image is loading, and still at `-28` after the 32 by 32 image arrives, until the sprite moves; then the apex is at 4.

The collision world indexes a cone by a box that is tight around the apex and the arc, not around the whole circle. For example, `Radius = 100` and `Angle = 30` with `Direction = 0` gives a box about 100 wide and 51.76 tall, not 200 by 200. For `Radius = 100`, `Angle = 270` and `Direction = 0` it is about 170.71 wide and 200 tall. The box follows the transform of the cone.

`Radius` must be finite and greater than zero, `Angle` must be greater than zero and at most 360, and `Direction` must be finite. A rejected value throws and leaves the old value in place. All three are UI properties.

### Motion

Motion can animate `Radius`, `Angle` and `Direction`, as it animates `CircleCollider2D.Radius`. The registry lists them with the same mixer (`FloatMixer`, a straight line between the two values), the same 180 ms tween as the default spec, and no implicit animation (see [AnimatablePropertyRegistry](Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry.md)). Each animated sample is a geometry change of the cone, so the collision world reads the cone again and contacts change at the next update.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Motion;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

// The sprite is in a scene that a RenderSurface2D shows, and the surface is in a UIRoot.
ConeCollider2D cone = new() { Radius = 80, Angle = 40 }; // Direction is not assigned
bee.Vision = cone;

cone.Motion()
    .Animate(ConeCollider2D.DirectionProperty)
    .To(90)
    .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));
// After 200 ms cone.Direction is 90, and the next contact delivery uses the cone that looks down.
```

- **Leave the animated property unassigned in code.** A property setter (`cone.Direction = 0`) writes the `Local` value, which outranks the `Animation` value that Motion writes, so the animation is hidden. Markup attributes and Aspect values are lower than `Animation`, so they do not hide it. The rule is the general one in [MotionPropertyBinding](Cerneala.UI.Motion.Properties.MotionPropertyBinding.md).
- **`Direction` turns in a straight line, not the short way.** It is interpolated like `UIElement.Rotation`: from 350 to 10 it passes through 180 and sweeps 340 degrees. To turn the short way, end beyond 360: from 350 to 370 it sweeps 20 degrees. `Direction` keeps the value as written (see above), so after that animation `cone.Direction` reads 370 and the cone looks along 10.
- **An invalid value is rejected, as for the radius of a circle.** An animated sample that the cone refuses is skipped and the value stays: `Radius` of zero or less or not finite, `Angle` of zero or less, over 360 or not finite, `Direction` not finite. A tween that overshoots past 360 on `Angle` therefore skips those samples. Starting an animation to an invalid target, such as `.To(400)` on `Angle`, throws `ArgumentOutOfRangeException`.
- **Motion transactions animate them too.** Inside `root.Motion.BeginTransaction(...)`, an `AspectBase` write to one of the three properties animates, instead of jumping, because the registry lists them.

### Where a cone can live

A cone can be owned in only one way: as the `Vision` of a [Sprite2D](Cerneala.UI.Controls.Sprite2D.md#vision) or of a [Scene2D](Cerneala.UI.Controls.Scene2D.md#vision). It is a logical child of that owner, in a slot of its own.

- `Sprite2D.Collider` refuses a cone. The assignment throws `InvalidOperationException` before anything changes, with a message that says to assign it to `Sprite2D.Vision` or `Scene2D.Vision`. A body already in `Collider` stays attached.
- `Scene2D.Children` and generic `LogicalChildren` or `VisualChildren` mutation refuse a cone, as they refuse every collider.
- One cone is the `Vision` of one owner at a time. Assigning it to a second owner throws `InvalidOperationException`. Set the first owner's `Vision` to `null`, then assign it to the second.
- Setting `Vision` to `null`, or to another cone, removes the old cone from its owner. The old cone has no owner afterwards and can be assigned again.
- A `Scene2D` owner places the cone with the group's transform: `X`, `Y`, `TransformOrigin` and the inherited transform channels. A `Sprite2D` owner places it with its `X`, `Y` and `Rotation`.

### Markup usage

In markup, a cone is declared inside the `Vision` property element of its owner: `<Sprite2D.Vision>` or `<Scene2D.Vision>`. Each takes exactly one `ConeCollider2D`, and the attributes of the cone are the same as for any element (`Radius`, `Angle`, `Direction`, `OffsetX`, `OffsetY`, ...). The generated code assigns the cone to the `Vision` property of the owner, so it is never the `Collider` of a sprite and never one of the `Children` of a group.

```xml
<Scene2D>
    <Sprite2D Width="32" Height="32">
        <BoxCollider2D Width="10" Height="10" OffsetX="-12" OffsetY="-5" />
        <Sprite2D.Vision>
            <ConeCollider2D Radius="150" Angle="90" Direction="0" />
        </Sprite2D.Vision>
    </Sprite2D>
</Scene2D>
```

This gives the sprite a body (`Collider` is the box) and a vision (`Vision` is a cone with `Radius` 150, `Angle` 90 and `Direction` 0). The sprite has two logical children. The order does not matter: the `<Sprite2D.Vision>` element can come before or after the box. A cone attribute that the markup leaves out keeps its default, so `<ConeCollider2D Radius="80" />` has `Angle` 90 and `Direction` 0.

A scene group has the same form, `<Scene2D.Vision>`:

```xml
<Scene2D>
    <Scene2D X="100" Y="50">
        <Scene2D.Vision>
            <ConeCollider2D Radius="150" Angle="90" Direction="0" />
        </Scene2D.Vision>
        <Sprite2D X="-40" Y="-5" Width="10" Height="10">
            <BoxCollider2D Width="10" Height="10" />
        </Sprite2D>
    </Scene2D>
</Scene2D>
```

The inner group has one child (the sprite), and its `Vision` is the cone. The outer root has no `Vision`. On the root of a [paired scene component](Cerneala.UI.Controls.Scene2D.md) (a `Scene2D` markup file with a class beside it), `<Scene2D.Vision>` sets the `Vision` of the component itself. A `Vision` does not count as the "one collider" of a sprite, so a body and a vision together are not an error.

The cases in this table are errors in the editor and in the build. Both report the same `CERNEALAUI005` with the same message:

| Markup | Message |
| --- | --- |
| `<Sprite2D><ConeCollider2D /></Sprite2D>` (a cone as the body) | `ConeCollider2D can only be a Vision; put it inside <Sprite2D.Vision> or <Scene2D.Vision>.` The build produces no generated code. |
| `<Scene2D><ConeCollider2D /></Scene2D>`, or a cone anywhere else that is not a `Vision` | The same message. |
| `<Sprite2D.Vision><BoxCollider2D /></Sprite2D.Vision>` (any other element in a `Vision`) | `Sprite2D.Vision accepts a ConeCollider2D only.` For a group: `Scene2D.Vision accepts a ConeCollider2D only.` |
| Two cones in one `Vision` | `Sprite2D.Vision accepts exactly one ConeCollider2D.` |
| `<Sprite2D.Vision />` with no cone | `Sprite2D.Vision requires exactly one ConeCollider2D child.` |
| Text, or an attribute, on the `Vision` element: `<Sprite2D.Vision>text<ConeCollider2D /></Sprite2D.Vision>` or `<Sprite2D.Vision Radius="5">...</Sprite2D.Vision>` | The same message as for no cone. Whitespace, comments and `xmlns` declarations are allowed. |
| Two `<Sprite2D.Vision>` elements in one sprite | `Sprite2D.Vision may be declared only once.` |

The full text of a build diagnostic is `Markup file 'World.crn' has invalid document shape: ` followed by the message, with the name of your own markup file in place of `World.crn`. Editor completion follows the same rules. It offers `ConeCollider2D` only inside an empty `<Sprite2D.Vision>` or `<Scene2D.Vision>`, never in the content of a `Sprite2D`, a `Tile` or a `Scene2D`. It offers the four body shapes in the content of a `Sprite2D`, and never inside a `Vision`.

### A Vision is a sensor

A cone in a `Vision` slot only senses. It does not block anything, nobody can click it, and only its owner learns what it touches.

| What | A `Vision` | Example |
| --- | --- | --- |
| Blocking | No | A fox that moves through the cone of a bee does not stop. `MoveAndCollide` of the fox returns no `Collision` and no `TriggerHits`. |
| Clicks | No | A click inside the cone, but outside the bee's body, does not hit the bee, and the cone is never the target of the click. |
| Touches | Detected, but only the owner learns | The fox enters the cone. `bee.Vision.Entered` is raised with `Other` set to the fox's collider. The fox's collider receives nothing. |

```csharp
using Cerneala.UI.Controls;

ConeCollider2D cone = new() { Radius = 150, Angle = 90, Direction = 0 };
BoxCollider2D foxBox = new() { Width = 10, Height = 10 };
Sprite2D bee = new()
{
    Collider = new BoxCollider2D { Width = 10, Height = 10, OffsetX = -12, OffsetY = -5 },
    Vision = cone
};
Sprite2D fox = new() { X = 400, Y = -5, Collider = foxBox };

Scene2D scene = new();
scene.Children.Add(bee);
scene.Children.Add(fox);
using SceneSimulationContext2D simulation = new(scene);

cone.Entered += (_, e) => Console.WriteLine("the bee sees the fox"); // e.Other is foxBox
foxBox.Entered += (_, e) => Console.WriteLine("never printed for the cone");

fox.X = 60;          // the fox box is now 60 to 70 units in front of the apex, inside the cone
simulation.Update(); // raises cone.Entered once; foxBox receives nothing
CollisionHit2D[] seen = scene.CollisionWorld.Overlap(cone); // one hit: foxBox
```

- **Events.** The sender of `Entered` and `Exited` is the cone, and `ColliderContactEventArgs.Other` is the body that arrived or left. A body never receives an event for a `Vision`, even when it has handlers, and the `Other` of any event is never a `Vision`. `IsTrigger` does not matter on a `Vision`: with `IsTrigger` set or not, the cone raises the same events and still blocks nothing. See [contact events](Cerneala.UI.Controls.Collider2D.md#contact-events).
- **Input skips it.** A `Vision` is not part of the input route and not a Servo target, and it adds nothing to the input bounds of its owner. For example, a Servo `ClickAsync` on the bee aims at the middle of the bee's own bounds, so it clicks the bee's body and not the empty place in the middle of the cone.
- **Queries of other colliders skip it.** `CollisionWorld2D.Overlap` of another collider, `Raycast` and `MoveAndCollide` never return a `Vision`. A `Vision` is never one of the `TriggerHits` of a move.
- **`Overlap(vision)` is the search.** `scene.CollisionWorld.Overlap(bee.Vision)` returns the colliders that touch the cone at this moment, in the usual attachment order. It never returns another `Vision`, and it never returns a collider inside the owner's subtree (see the next table). With the example above, `Overlap(bee.Vision)` is empty while the fox is at `X = 400`, and has the fox's collider and `Entity` = `fox` while the fox is at `X = 60`.
- **`Intersects` works.** `Intersects(fox.Collider, bee.Vision)` is `true` while the fox box and the cone touch, in either argument order. `Intersects(visionA, visionB)` is `false` for any two `Vision` cones, and `Intersects(bee.Vision, bee.Collider)` is `false`.
- **A `Vision` cannot be moved.** `MoveAndCollide(bee.Vision, displacement)` throws `InvalidOperationException` with a message that names `Vision`. It throws when the cone is enabled or not, and when its owner is not in the scene of the world. Move the owner instead.
- **Layers and masks apply.** A cone sees a body only when the cone's `CollisionMask` accepts the body's `CollisionLayer` and the body's `CollisionMask` accepts the cone's `CollisionLayer`, as for any pair of colliders.
- **The overlay draws it apart.** The debug overlay draws a `Vision` in its own colour. See [Scene2DDebugOverlay](Cerneala.UI.Controls.Scene2DDebugOverlay.md).

#### A Vision never sees its owner

A `Vision` does not see the colliders that belong to its owner. What counts as belonging depends on the owner:

| The owner is | The `Vision` ignores | Example |
| --- | --- | --- |
| A `Sprite2D` | That sprite's own `Collider` | `bee.Vision` never reports `bee.Collider`, even where the body overlaps the cone. It still sees the body of another sprite that stands at the same place. |
| A `Scene2D` | Every collider inside the group, also inside nested groups | `dragon.Vision` ignores the bodies of all the dragon's parts, and of a nested tail group. It sees a fox that is outside the group. |
| A `Sprite2D` inside a group | Only that sprite's own `Collider` | `head.Vision`, where `head` is a sprite of the dragon, ignores `head.Collider` and sees the dragon's wing. |

```csharp
using Cerneala.UI.Controls;

Scene2D dragon = new() { X = 100, Y = 50, Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 } };
Sprite2D head = new()
{
    Collider = new BoxCollider2D { Width = 10, Height = 10 },
    Vision = new ConeCollider2D { Radius = 100, Angle = 90, Direction = 0 }
};
Sprite2D wing = new() { X = 40, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } };
dragon.Children.Add(head);
dragon.Children.Add(wing);
// head.Vision sees wing.Collider and does not see head.Collider.
// dragon.Vision sees neither of them: both are inside the dragon group.
```

Two `Vision` cones never see each other, also when one is the cone of a group and the other the cone of a sprite in that group: each one reports the bodies that it sees on its own. A `Vision` on a root `Scene2D` sees nothing, because every collider of that scene is inside the scene.

## Constructors

| Name | Description |
| --- | --- |
| `ConeCollider2D()` | Creates a cone collider with radius `100`, angle `90` and direction `0`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `RadiusProperty` | `UiProperty<float>` | Identifies the `Radius` UI property. |
| `AngleProperty` | `UiProperty<float>` | Identifies the `Angle` UI property. |
| `DirectionProperty` | `UiProperty<float>` | Identifies the `Direction` UI property. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `100` | Gets or sets how far the cone reaches from its apex. Finite and greater than zero. |
| `Angle` | `float` | `90` | Gets or sets how wide the cone is, in degrees. Greater than 0 and at most 360; 360 is a full circle. |
| `Direction` | `float` | `0` | Gets or sets where the cone looks, in degrees. 0 looks right, 90 looks down. Any finite value; kept as set and used normalized. |

## Exceptions

Setting `Radius` to zero, a negative value, `NaN`, or infinity throws `ArgumentOutOfRangeException`.

Setting `Angle` to zero, a negative value, a value above `360`, `NaN`, or infinity throws `ArgumentOutOfRangeException`.

Setting `Direction` to `NaN` or infinity throws `ArgumentOutOfRangeException`.

Assigning a cone to `Sprite2D.Collider`, to a second owner while it still has one, or to `Scene2D.Children` throws `InvalidOperationException`. In markup, a cone outside `Sprite2D.Vision` and `Scene2D.Vision` reports `CERNEALAUI005` instead (see [Markup usage](#markup-usage)).

Passing a cone that is some owner's `Vision` to `CollisionWorld2D.MoveAndCollide` throws `InvalidOperationException`, with a message that names `Vision`.

## Applies to

Project: `Cerneala`

## See also

- `Collider2D`
- `CollisionWorld2D`
- `Scene2DDebugOverlay`
- `Sprite2D`
- `Scene2D`
- `CircleCollider2D`
- `BoxCollider2D`
- `PolygonCollider2D`
