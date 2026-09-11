# Tile Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Tile.cs`

An immutable, freely positioned image placement in a [TileMap2D](Cerneala.UI.Controls.TileMap2D.md).

```csharp
public sealed class Tile
```

## Examples

```xml
<TileMap2D>
  <Tile Image="$Grass" X="0" Y="0" />
  <Tile Image="$House" X="210" Y="125" Width="24" Height="20" />
</TileMap2D>
```

`Grass` and `House` must be declared `ImageResource` resources in the enclosing resource scope.

```csharp
var model = new TileMap2DModel(new[]
{
    new Tile(new ImageReference(new ResourceId<ImageResource>("Grass"))),
    new Tile(new ImageReference(new ResourceId<ImageResource>("House")),
        x: 210, y: 125, width: 24, height: 20)
});
```

## Remarks

Coordinates are local pixel positions, not cell indices. Negative and fractional positions are supported. Each omitted dimension uses the resolved image's corresponding natural dimension independently; supplying only `Width` does not scale `Height` proportionally. The complete image is drawn into the destination rectangle. Zero dimensions produce no visible area.

Placements draw in declaration order, including when different images overlap. They do not require a grid, tile IDs, a tileset, or manually authored chunks.

`Tile` is not a `UIElement` or `SceneNode2D`. It has no per-placement events, bindings, Aspect, Motion, Prism, lifecycle, or promotion state. Apply scene behavior to the containing map; use `Sprite2D` for individually interactive or animated images. Markup accepts only the five attributes below, with an image resource reference and literal numbers. A tile must be a direct child of `TileMap2D`.

Construction does not load or take ownership of resource images. The map resolves them through its resource scope. C# callers may also provide a direct image through `ImageReference`; ownership remains with the caller. To change a placement, publish a replacement immutable model.

Coordinates and explicit dimensions must fit the drawing pixel range; dimensions cannot be negative or infinite. C# uses `float.NaN` for an omitted dimension. Geometry that depends on natural image dimensions is checked after image resolution.

## Constructors

| Name | Description |
| --- | --- |
| `Tile(ImageReference, float, float, float, float)` | Creates a placement. `x` and `y` default to zero; `width` and `height` default to natural size. |

## Properties

All properties are read-only.

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `ImageReference` | Required image source. |
| `X` | `float` | Local left coordinate. |
| `Y` | `float` | Local top coordinate. |
| `Width` | `float` | Explicit destination width, or `NaN` for natural width. |
| `Height` | `float` | Explicit destination height, or `NaN` for natural height. |

## See also

- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [ImageReference](Cerneala.UI.Resources.ImageReference.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
