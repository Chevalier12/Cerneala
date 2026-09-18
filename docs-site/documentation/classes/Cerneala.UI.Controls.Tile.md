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
  <Tile Image="$Grass" X="0" Y="0" ImageWidth="32" ImageHeight="32" />
  <Tile Image="$House" X="210" Y="125" Width="24" Height="20">
    <BoxCollider2D Width="24" Height="4" OffsetY="16" CollisionLayer="2" />
  </Tile>
</TileMap2D>
```

`Grass` and `House` must be declared `ImageResource` resources in the enclosing resource scope.

```csharp
var model = new TileMap2DModel(new[]
{
    new Tile(new ImageReference(new ResourceId<ImageResource>("Grass"))),
    new Tile(new ImageReference(new ResourceId<ImageResource>("House")),
        new TileColliderDescriptor2D(TileColliderShape2D.Box, width: 24, height: 4, offsetY: 16, collisionLayer: 2),
        x: 210, y: 125, width: 24, height: 20)
});
```

## Remarks

Coordinates are local pixel positions, not cell indices. Negative and fractional positions are supported. Each omitted dimension uses the source catalog's declared image dimension independently; supplying only `Width` does not scale `Height` proportionally. The complete image is drawn into the destination rectangle. Zero dimensions produce no visible area.

Placements draw in declaration order, including when different images overlap. They do not require a grid, tile IDs, a tileset, or manually authored chunks.

`Tile` is not a `UIElement` or `SceneNode2D`. It has no per-placement events, bindings, Aspect, Motion, Prism, lifecycle, or promotion state. Apply scene behavior to the containing map; use `Sprite2D` for individually interactive or animated free placements. A tile must be a direct child of `TileMap2D`. Its five placement attributes accept an image resource reference and literal numbers. The markup-only `ImageWidth` and `ImageHeight` attributes declare intrinsic metadata for that image throughout the containing map; they are not CLR properties on `Tile`. Supply both together as finite positive literals no greater than 2,000,000,000. Repeated equal declarations are allowed; conflicting declarations are errors. A declaration may appear after another placement using that image. Each omitted destination axis requires this metadata; explicitly supplying both destination dimensions does not. The compiler lowers the declarations to `TileMapSource2D.FromModel` and its `imageSizes` argument without decoding an image.

`Collider` is an optional immutable `TileColliderDescriptor2D`. In markup, one direct `BoxCollider2D`, `CircleCollider2D`, `PolygonCollider2D`, or `SegmentCollider2D` child constructs that descriptor, not a live UI node. A second shape child is rejected rather than ignored or merged. Common literal attributes are `OffsetX`, `OffsetY`, `CollisionLayer`, `CollisionMask`, and `IsTrigger`; shape-specific attributes are `Width`/`Height`, `Radius`, `Points`, or `EndX`/`EndY`, respectively. Bindings, names, events, Aspect, Motion, Prism, inherited UI transforms, and `Enabled` are not accepted on static descriptors. Use `CollisionLayer="0"` for nonparticipating static geometry, or publish a replacement model. Use `Sprite2D` for live collider state.

The shape uses destination units relative to the tile's `X`/`Y`, followed by containing map/scene transforms. Image dimensions do not resize it. A C# descriptor may also supply its own `LocalTransform`. The map retains collision adapters for camera, simulation and explicitly prepared region interests independently of image residency; a static placement is not a per-tile input element.

Construction does not load or take ownership of resource images. The map resolves them through its resource scope. C# callers may also provide a direct image through `ImageReference`; ownership remains with the caller. To change a placement, publish a replacement immutable model.

Coordinates and explicit dimensions must fit the drawing pixel range; dimensions cannot be negative or infinite. C# uses `float.NaN` for an omitted dimension. Geometry that depends on natural image dimensions is checked using source metadata before acquiring payloads or decoding images.

## Constructors

| Name | Description |
| --- | --- |
| `Tile(ImageReference, float, float, float, float)` | Creates a placement. `x` and `y` default to zero; `width` and `height` default to natural size. |
| `Tile(ImageReference, TileColliderDescriptor2D?, float, float, float, float)` | Creates a placement with an optional immutable collider descriptor. The same position and dimension defaults apply. |

## Properties

All properties are read-only.

| Name | Type | Description |
| --- | --- | --- |
| `Image` | `ImageReference` | Required image source. |
| `X` | `float` | Local left coordinate. |
| `Y` | `float` | Local top coordinate. |
| `Width` | `float` | Explicit destination width, or `NaN` for natural width. |
| `Height` | `float` | Explicit destination height, or `NaN` for natural height. |
| `Collider` | `TileColliderDescriptor2D?` | Optional immutable tile-local collision descriptor; `null` for the original constructor. |

## Exceptions

A null image throws `ArgumentNullException`. Invalid transformed collider geometry is rejected. Each placement accepts at most one descriptor by construction, and a free-placement model accepts at most 65,536 expanded colliders (`SCN2D013`). The descriptor is immutable, so changing collision geometry requires a replacement tile/model.

## See also

- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [ImageReference](Cerneala.UI.Resources.ImageReference.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
