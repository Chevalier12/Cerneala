# TileMap2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2D.cs`

Records one retained stratum of static tile data inside a scene, without creating a public scene node for each tile.

```csharp
public sealed class TileMap2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `TileMap2D`

## Examples

```xml
<RenderSurface2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
  <RenderSurface2D.Resources>
    <r:ImageResource Name="Grass" Source="grass.png" />
    <r:ImageResource Name="House" Source="house.png" />
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D OrderMode="Layer">
      <TileMap2D Layer="0">
        <Tile Image="$Grass" X="0" Y="0" />
        <Tile Image="$House" X="210" Y="125" Width="24" Height="20" />
      </TileMap2D>
      <Sprite2D Layer="1" Image="$House" X="250" Y="125" Width="24" Height="20">
        <BoxCollider2D Width="24" Height="20" />
      </Sprite2D>
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

Direct `Tile` declarations use pixel positions and optional independent dimensions. Each omitted dimension comes from the corresponding image dimension. Free placements may overlap; declaration order is painter order.

For imported data, compose the level's independent models as peer maps:

```csharp
var imported = TiledScene2DImporter.Import("village.tmj");
if (!imported.Success)
    throw new InvalidOperationException(string.Join(Environment.NewLine, imported.Diagnostics));
Scene2DLevel level = imported.Document!.Levels.Single();
var scene = new Scene2D
{
    OrderMode = SceneOrderMode.Layer,
    TranslateX = level.WorldOffset.X,
    TranslateY = level.WorldOffset.Y
};
foreach (TileMap2DModel model in level.TileMaps)
    scene.Children.Add(new TileMap2D { Model = model, Layer = model.Order });
```

Composition must also register the document's declared image resources in the enclosing resource scope. Import does not load atlas pixels or create UI nodes.

## Remarks

A map owns one `Model`, not a collection of presentation layers. Compose multiple maps, individual [Sprite2D](Cerneala.UI.Controls.Sprite2D.md) nodes, and [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) collections through [Scene2D](Cerneala.UI.Controls.Scene2D.md). The containing scene interprets the inherited `Layer` property. `Model.Order` is composition metadata; assigning a model does not copy it to `Layer`.

In markup, either declare direct static `Tile` children or bind `Model`; do not mix the two. A map cannot contain live sprites or scene groups. `Model` property-element wrappers are not supported. For shared transforms, opacity, or Prism around maps and sprites, put them in a common `Scene2D`.

### Retained drawing and presentation

Free placements are grouped internally into bounded chunks. Imported grids supply immutable chunks directly. Recording derives a conservative visible region from the ViewBox and scene transforms, queries the sparse chunk index, and emits intersecting chunks. Cached batches survive unchanged frames and camera transform changes. Grid cells are grouped by atlas within a chunk; free placements coalesce only adjacent equal-image runs so overlap order is preserved.

Map Aspect, Motion, Prism, transforms, opacity, visibility, binding, and attachment use the normal scene/UI paths. Model offset is added to the presentation offset; model and presentation tint and opacity compose. Static tiles do not have independent events, animation, effects, binding, or lifecycle state.

Use an ordinary peer sprite for an individual interactive or animated object. If that sprite replaces a grid cell, application composition must publish a model with that cell cleared and configure the sprite's image, pixel position, collider, and scene order. Adding a sprite does not suppress static data automatically. Restoring the original immutable model and removing the sprite restores the static cell.

### Static tile colliders

A free placement's optional `Tile.Collider` is immutable data. Its markup shape child creates a descriptor, not a live collider node. Geometry follows the placement's pixel position and the containing map/scene transforms; image dimensions do not resize it.

The optional `TileDefinition2D.Collider` supplies geometry for each matching non-empty grid cell. The map owns internal collision adapters. Collision hits identify the map rather than a public per-cell entity, and application child-collection mutation cannot reparent those adapters. Visual culling does not unload collision geometry.

Grid collision synchronization is chunk-local. Reuse compares cell IDs and flips, tile size, visibility, and referenced collider descriptors. Replacing a chunk with identical cell data does not by itself recreate its adapters. Descriptor absence is also tracked, so adding a collider to a previously noncolliding definition updates dependent chunks. These collision checks do not replace the drawing-version requirements below. Adjacent horizontal full-cell boxes may coalesce only when geometry and collision semantics match. Chunk boundaries and differing metadata remain boundaries.

### Mutation and versioning

For free placements, assign a replacement `TileMap2DModel(IEnumerable<Tile>)`; changed placement lists are detected even with the default publication version.

For grids, construct replacement chunks/models and change the positive versions of modified data. A changed chunk needs a changed `TileChunk2D.Version`; changed definitions or atlas identity need a changed tileset or resource version. Reusing a cache-visible version for different grid data is unsupported. Unchanged chunks and atlas dependencies retain their batches. Presentation transforms, offsets, and opacity do not rebuild static geometry; changing tint changes batch color data.

### Limits

- Free placements draw complete images without per-tile cropping or flips and have no uniform grid size.
- Grid tile IDs are global across tilesets in one model; ID `0` is empty.
- Chunks in one map cannot overlap. Finite bounds contain every chunk; null bounds allow sparse negative/remote coordinates without allocating the gaps.
- Shared atlas images are resolved through the normal resource system and are not owned or disposed by the map.
- Parsers, navigation, animation, and pointer input remain separate facilities. Batch-only cells are not individual input elements.

## Constructors

| Name | Description |
| --- | --- |
| `TileMap2D()` | Creates an empty map with a null model. |

## Properties

| Name | Default | Description |
| --- | --- | --- |
| `Model` | `null` | Immutable/versioned static data for this map. |
| `Offset` | `default` | Pixel translation added to the model offset. |
| `Tint` | `Color.White` | Presentation tint composed with model tint. |
| `TransformOrigin` | `default` | Local scene-space origin for inherited transforms. |
| `Layer` (inherited) | `0` | Ordering key interpreted by the containing scene. |

Each declared property has a corresponding public `...Property` UI-property field.

## See also

- [Tile](Cerneala.UI.Controls.Tile.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [Sprite2D](Cerneala.UI.Controls.Sprite2D.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)
