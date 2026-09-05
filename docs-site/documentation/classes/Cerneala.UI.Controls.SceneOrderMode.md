# SceneOrderMode Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneOrderMode.cs`

Specifies how a `Scene2D` creates its stable recording order without modifying its child collection.

```csharp
public enum SceneOrderMode
```

## Examples

```xml
<Scene2D OrderMode="LayerThenY">
    <Sprite2D Layer="0" />
    <Sprite2D Layer="10" />
</Scene2D>
```

## Remarks

All modes preserve `Scene2D.Children`. Sorting is a recording view, not a mutation of the logical tree or source collection.

Layer and Y values are ordered from smaller to larger. Equal keys preserve source collection order. `LayerThenY` uses the bottom edge of transformed scene-space bounds; an unknown bound uses `0` as its Y anchor. `Sprite2D.LayerDepth` is a drawing-backend value and does not participate in these modes.

Aspect can switch `Scene2D.OrderMode` as structural state. Motion does not animate this enum and reports a generated-markup diagnostic when it is used as an animation destination.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Source` | `0` | Records children in source collection order. This is the default. |
| `Layer` | `1` | Sorts by `SceneNode2D.Layer`, then by source order. |
| `LayerThenY` | `2` | Sorts by layer, transformed scene-space bottom edge, then source order. |

## Applies to

Project: `Cerneala`

## See also

- `Scene2D`
- `SceneNode2D`
- `Sprite2D`
