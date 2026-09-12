# PolygonCollider2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/PolygonCollider2D.cs`

Defines a strictly convex polygon collision shape in local scene coordinates.

```csharp
public sealed class PolygonCollider2D : Collider2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Collider2D` -> `PolygonCollider2D`

## Examples

```xml
<Sprite2D X="32" Y="16">
    <PolygonCollider2D Points="0,0 10,0 12,8 0,8" />
</Sprite2D>
```

## Remarks

Live polygons attach only through `Sprite2D.Colliders` or `TileInstance2D.Colliders`; scene groups and generic UI child collections cannot own them. The sprite example has collision geometry but no image. A polygon directly under a static `Tile` constructs an immutable descriptor, with no live binding or Aspect state. See [Collider2D](Cerneala.UI.Controls.Collider2D.md) and [Tile](Cerneala.UI.Controls.Tile.md).

`Points` uses invariant `x,y x,y ...` syntax. Clockwise and counterclockwise input are accepted, but the polygon must be simple and strictly convex: every nonincident vertex lies on the interior side of every edge. Consistent local turn signs alone are insufficient because self-intersecting stars can satisfy them. Concave, self-intersecting, collinear, empty, malformed, and nonfinite shapes are rejected instead of being approximated.

At most 4,096 points and 393,216 UTF-16 characters are accepted. Coordinates must fit the drawing coordinate range and cross products must remain finite and exceed the existing degeneracy epsilon. The same parser and validation rules are used by tile collider descriptors and scene entity polygons. See [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md) for diagnostic codes and limits.

`Vertices` exposes the parsed points as a read-only list. The list is replaced only after a new `Points` value passes validation, so a rejected mutation does not install partial geometry.

Aspect, bindings, direct assignment, and `@set` can replace `Points`. The property is deliberately discrete and cannot be interpolated by Motion because no polygon topology mixer is defined. Inherited transform channels remain animatable.

## Constructors

| Name | Description |
| --- | --- |
| `PolygonCollider2D()` | Creates an enabled unit-triangle collider with points `0,0 1,0 0,1`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `PointsProperty` | `UiProperty<string>` | Identifies the polygon-source UI property. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Points` | `string` | `0,0 1,0 0,1` | Gets or sets the invariant convex-polygon source text. |
| `Vertices` | `IReadOnlyList<Vector2>` | Unit triangle | Gets the validated read-only vertex sequence. |

## Exceptions

Setting `Points` to an empty or malformed string, fewer than three points, nonfinite coordinates, a degenerate polygon, or a concave polygon throws `ArgumentException`.

## Applies to

Project: `Cerneala`

## See also

- `Collider2D`
- `BoxCollider2D`
- `CircleCollider2D`
