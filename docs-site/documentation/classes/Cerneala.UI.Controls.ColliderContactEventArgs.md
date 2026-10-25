# ColliderContactEventArgs Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ColliderContactEventArgs.cs`

Provides data for the `Collider2D.Entered` and `Collider2D.Exited` events.

```csharp
public sealed class ColliderContactEventArgs : EventArgs
```

Inheritance:
`object` -> `EventArgs` -> `ColliderContactEventArgs`

## Examples

The two boxes already overlap when the handler is added. Adding the handler raises nothing; the next update raises `Entered` once.

```csharp
using Cerneala.UI.Controls;

BoxCollider2D coinBox = new() { Width = 10, Height = 10, IsTrigger = true };
BoxCollider2D foxBox = new() { Width = 10, Height = 10 };

Scene2D scene = new();
scene.Children.Add(new Sprite2D { Collider = coinBox });
scene.Children.Add(new Sprite2D { X = 5, Collider = foxBox });
using SceneSimulationContext2D simulation = new(scene);

coinBox.Entered += (sender, e) =>
{
    // sender is coinBox, the collider that receives the event.
    // e.Other is foxBox, the collider on the other side of the contact.
    Collider2D other = e.Other;
};

simulation.Update(); // raises coinBox.Entered once, with e.Other == foxBox
```

## Remarks

The collision world of a root `Scene2D` creates `ColliderContactEventArgs` when it delivers contact changes. The sender of `Entered` or `Exited` is the collider that receives the event, and `Other` is the collider on the other side of the contact. When both colliders have handlers, each one receives its own event: the coin receives `Other` set to the fox's collider, and the fox receives `Other` set to the coin's collider.

When the other side is a static tile, `Other` is the map's internal tile collider: the same `Collider2D` object that `CollisionWorld2D.Overlap` reports in `CollisionHit2D.Collider` for that tile.

The constructor throws `ArgumentNullException` when `other` is `null`.

See [Collider2D contact events](Cerneala.UI.Controls.Collider2D.md#contact-events) for the contact rule, the delivery point, and the event order.

## Constructors

| Name | Description |
| --- | --- |
| `ColliderContactEventArgs(Collider2D other)` | Initializes the event data with the other collider of the contact. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Other` | `Collider2D` | Gets the collider on the other side of the contact. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.Collider2D`
- `Cerneala.UI.Controls.CollisionWorld2D`
- `Cerneala.UI.Controls.CollisionHit2D`
- `Cerneala.UI.Controls.SceneSimulationContext2D`
