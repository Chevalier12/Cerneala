# TileMap2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2D.cs`

Represents a retained tile map inside a `Scene2D` without creating one scene node for every static tile.

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
    <Scene2D>
      <TileMap2D>
        <Tile Image="$Grass" X="0" Y="0" />
        <Tile Image="$House" X="210" Y="125" Width="24" Height="20" />
      </TileMap2D>
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

This is the supported `.crn` authoring form: direct `Tile` declarations with `Image`, `X`, `Y`, and optional independent `Width` and `Height`. Positions are pixels, not grid cells. Each omitted dimension comes from the corresponding image dimension. Images may have different sizes, and placements may overlap; declaration order is painter order.

Tiled/LDtk imports remain available through the C# model API:

```csharp
var result = TiledScene2DImporter.Import("village.tmj");
if (!result.Success)
    throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
map.Model = result.Document!.Levels.Single().TileMap;
```

Declare the imported model's image resources in the map's enclosing scope. Bind the imported `Model` in markup and declare `TileLayer2D` presentations with sparse `TileInstance2D` children when a layer or promoted cell needs its own Aspect, Motion, Prism, binding, or input behavior. These declarations address existing imported data; they do not author a grid or matrix. Do not combine free `Tile` placements with a `Model` binding or layer presentations in the same map. `Model`/`Layers` property-element wrappers are not supported.

## Remarks

`Model` supplies immutable image placements or imported/versioned grid data. Static tiles remain compact data. Free placements are grouped automatically into bounded retained draw chunks; applications do not author those chunks. Recording first derives a conservative visible region from the surface ViewBox and the composed scene transforms, queries the sparse chunk index, and emits only intersecting chunks. Cached static batches survive an unchanged frame and a camera transform change.

`Layers` contains one high-level `TileLayer2D` presentation node per model layer, not one node per tile. Matching layer nodes are generated from the model. Applications may address those nodes from C# or declare layer presentations in markup for imported maps. Free-placement maps have one generated layer. Model `Order`, followed by model source order for ties, controls ordering inside the map.

Map, layer, and promoted-tile nodes use the normal Aspect, Motion, Prism, transform, opacity, visibility, and attachment paths inherited from the scene/UI tree. Static cells do not have independent Aspect, Motion, Prism, event, or lifecycle state.

### Static tile colliders

Free placements use `Tile.Colliders`. Their markup shape children construct immutable descriptors, not live `Collider2D` nodes. Adapters use the tile's pixel position and explicit shape dimensions; natural or explicit image dimensions do not rescale collision geometry. Unchanged free-placement models retain their adapters, while replacing the model rebuilds that placement geometry. See [Tile](Cerneala.UI.Controls.Tile.md) for literal authoring and limits.

Each `TileDefinition2D.Colliders` entry is adapted into scene-owned collision geometry for every matching non-empty cell. Static cells remain compact model data; the adapter does not promote them into public per-cell UI elements and does not send collider forms to a graphics backend.

For imported grids, collision synchronization is chunk-local. Replacing one chunk or a collider definition rebuilds only dependent chunk adapters, while unchanged chunk objects retain their collision entries. Removing a chunk removes its entries. Visual ViewBox culling affects drawing only and does not unload active colliders from the collision world. Internal adapters are owned by their originating tile data and exact presentation layer; application child-collection mutation cannot reparent them.

Adjacent horizontal full-cell box descriptors may share one internal collider only when geometry, offsets, layer, mask, trigger state, importer properties, and debug identity are semantically identical. Chunk boundaries and differing metadata remain boundaries.

### Promotion and demotion

For imported grid models, `Promote` extracts one addressed cell from its static batch and returns its unique `TileInstance2D`. A non-empty cell inherits its tile ID, atlas source rectangle, and flip. An explicit `tileId` can replace that visual; it is required when ID `0`, the empty value, is promoted. The promoted node is recorded in the same row-major semantic slot, so promotion does not draw the cell twice or move it above unrelated content.

Calling `Promote` again for an already promoted key returns the existing instance. It does not replace that instance or apply a new `tileId`. Use the returned node's public properties for visual overrides. `Demote` detaches and removes the instance, returns the model cell to the static batch, and returns `false` when the key is not currently promoted. `TryGetPromoted` performs a non-mutating lookup.

Promotion can split one cached static order segment into a segment before the instance and a segment after it. A per-tile Prism scope also occupies that individual slot. The cost therefore scales with promoted positions and atlas segments, while zero promotions preserve the compact batch path. Promote only cells that need individual behavior.

Explicit colliders on a promoted tile compose with imported descriptors by default. Set `TileInstance2D.ReplacesImportedColliders` to `true` when the promoted tile owns the complete replacement; demotion restores the imported descriptors.

### Mutation and versioning

Model objects copy their collection inputs and expose read-only views. For free placements, construct a replacement `TileMap2DModel(IEnumerable<Tile>)` and assign it to `Model`; changed placement lists are detected even when the default publication version is reused. For imported grids, the version contract below remains unchanged. To change tile content, construct replacement `TileChunk2D`, `TileLayer2DModel`, and `TileMap2DModel` objects, increment the positive version of changed data, and assign the replacement model to `Model`. Reusing a version for changed tile or atlas data is unsupported because the retained cache uses chunk, tileset, and resource versions to decide whether a batch is current.

Changing only scene presentation properties such as map/layer transforms, opacity, or offsets does not rebuild static tile geometry. A changed chunk rebuilds its dependent cached segments; a changed tileset/resource invalidates only batches that reference it.

### Limits

- Free placements have individual pixel destinations and no uniform `TileSize`. Imported grid models retain their uniform destination cell size.
- For imported grids, tile IDs are global across all tilesets in one model. ID `0` is reserved for an empty cell.
- Free placements draw complete images without per-tile flips or cropping. Imported grid cells retain their existing flip metadata.
- Imported grid chunks within one layer may not overlap. A finite map rejects chunks outside its bounds; a null bounds value represents a sparse map, not an eagerly allocated infinite rectangle.
- Atlas resources must be resolvable through the normal `ImageResource` system. The tile map does not own or dispose shared atlas images.
- Tiled/LDtk parsing, sprite-frame animation, navigation, and geometric pointer picking are separate facilities. Tile collision descriptors and promoted colliders use this map's scene-owned collision adapter; importers remain responsible for translating external metadata into those public descriptors.

## Constructors

| Name | Description |
| --- | --- |
| `TileMap2D()` | Creates an empty map node with an addressable layer collection. |

## Properties

| Name | Description |
| --- | --- |
| `Model` | Gets or sets the immutable/versioned tile map model. |
| `Layers` | Gets the high-level layer presentation nodes; imported layer presentations can also be declared as direct markup children. |
| `TransformOrigin` | Gets or sets the scene-space origin used by inherited transform properties. |

## Methods

| Name | Description |
| --- | --- |
| `Promote(TileCellKey2D, int?)` | Returns the unique promoted node for a cell, optionally replacing an empty cell with a tile ID. |
| `Demote(TileCellKey2D)` | Removes a promoted node and returns its cell to static recording. |
| `TryGetPromoted(TileCellKey2D, out TileInstance2D?)` | Finds the promoted node for a stable cell key. |

## Applies to

Project: `Cerneala`

## See also

- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
- [TileLayer2D](Cerneala.UI.Controls.TileLayer2D.md)
- [TileInstance2D](Cerneala.UI.Controls.TileInstance2D.md)
- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)
- [Scene2D](Cerneala.UI.Controls.Scene2D.md)
