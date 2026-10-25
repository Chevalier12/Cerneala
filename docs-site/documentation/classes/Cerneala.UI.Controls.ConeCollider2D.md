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

When the owner is a `Sprite2D`, the cone uses the owner's `Flip` for its shape. `Flip` mirrors `Direction` only. `OffsetX` and `OffsetY` are not mirrored, so the apex stays where it is. This differs from the other colliders, whose geometry ignores `Flip`. When `Flip` changes, the collision world reads the cone's geometry again. A cone owned by a `Scene2D` is never flipped, because `Scene2D` has no `Flip` property.

| `Flip` | `Direction` becomes | Example: `Direction = 30` |
| --- | --- | --- |
| `None` | `Direction` | `30` |
| `Horizontal` | `180 - Direction` | `150` |
| `Vertical` | `-Direction` | `-30` |
| `Horizontal` and `Vertical` | `Direction + 180` | `210` |

The collision world indexes a cone by a box that is tight around the apex and the arc, not around the whole circle. For example, `Radius = 100` and `Angle = 30` with `Direction = 0` gives a box about 100 wide and 51.76 tall, not 200 by 200. For `Radius = 100`, `Angle = 270` and `Direction = 0` it is about 170.71 wide and 200 tall. The box follows the transform of the cone.

`Radius` must be finite and greater than zero, `Angle` must be greater than zero and at most 360, and `Direction` must be finite. A rejected value throws and leaves the old value in place. All three are UI properties.

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
