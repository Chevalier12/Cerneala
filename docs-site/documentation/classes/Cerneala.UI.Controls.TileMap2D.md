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
<RenderSurface2D DataType="Cerneala.UI.Controls.TileMap2DModel">
  <RenderSurface2D.Scene>
    <Scene2D>
      <TileMap2D Model="$DataContext:OneWay">
        <TileMap2D.Aspect>
          @on Loaded
          {
            @animate with Tween(100ms)
            {
              @to { Opacity = 0.9; }
            }
          }
        </TileMap2D.Aspect>
        @prism
        {
          @layer MapContent
          {
            Opacity = 1;
            @filter Blur { Radius = 1; }
          }
        }
        <TileLayer2D LayerId="Buildings">
          <TileInstance2D X="18" Y="11">
            <TileInstance2D.Aspect>
              @on Loaded
              {
                @animate with Tween(100ms)
                {
                  @to { Tint = #FFFFCC; }
                }
              }
            </TileInstance2D.Aspect>
            @prism
            {
              @layer DoorEffect
              {
                Opacity = 1;
                @filter Blur { Radius = 2; }
              }
            }
          </TileInstance2D>
        </TileLayer2D>
      </TileMap2D>
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

This is Cerneala `.crn` syntax, not XAML syntax inferred from another framework. The root element must declare a compatible `DataType` when `$DataContext` binding is used. The bound model is normally created by application code or an importer; bulk tile data is not expanded into one markup element per cell.

## Remarks

`Model` supplies immutable/versioned tilesets, layers, and chunks. Static cells remain compact data. Recording first derives a conservative visible region from the surface ViewBox and the composed scene transforms, queries the sparse chunk index, and emits only intersecting chunks. Cached static batches survive an unchanged frame and a camera transform change.

`Layers` contains one high-level `TileLayer2D` presentation node per model layer, not one node per tile. A matching node is generated when markup does not declare one. Declaring a layer is necessary only when the application needs to address that layer or add promoted cells. Model `Order`, followed by model source order for ties, controls ordering inside the map.

Map, layer, and promoted-tile nodes use the normal Aspect, Motion, Prism, transform, opacity, visibility, and attachment paths inherited from the scene/UI tree. Static cells do not have independent Aspect, Motion, Prism, event, or lifecycle state.

### Static tile colliders

Each `TileDefinition2D.Colliders` entry is adapted into scene-owned collision geometry for every matching non-empty cell. Static cells remain compact model data; the adapter does not promote them into public per-cell UI elements and does not send collider forms to a graphics backend.

Collision synchronization is chunk-local. Replacing one chunk or a collider definition rebuilds only dependent chunk adapters, while unchanged chunk objects retain their collision entries. Removing a chunk removes its entries. Visual ViewBox culling affects drawing only and does not unload active colliders from the collision world.

Adjacent horizontal full-cell box descriptors may share one internal collider only when geometry, offsets, layer, mask, trigger state, importer properties, and debug identity are semantically identical. Chunk boundaries and differing metadata remain boundaries.

### Promotion and demotion

`Promote` extracts one addressed cell from its static batch and returns its unique `TileInstance2D`. A non-empty cell inherits its tile ID, atlas source rectangle, and flip. An explicit `tileId` can replace that visual; it is required when ID `0`, the empty value, is promoted. The promoted node is recorded in the same row-major semantic slot, so promotion does not draw the cell twice or move it above unrelated content.

Calling `Promote` again for an already promoted key returns the existing instance. It does not replace that instance or apply a new `tileId`. Use the returned node's public properties for visual overrides. `Demote` detaches and removes the instance, returns the model cell to the static batch, and returns `false` when the key is not currently promoted. `TryGetPromoted` performs a non-mutating lookup.

Promotion can split one cached static order segment into a segment before the instance and a segment after it. A per-tile Prism scope also occupies that individual slot. The cost therefore scales with promoted positions and atlas segments, while zero promotions preserve the compact batch path. Promote only cells that need individual behavior.

Explicit colliders on a promoted tile compose with imported descriptors by default. Set `TileInstance2D.ReplacesImportedColliders` to `true` when the promoted tile owns the complete replacement; demotion restores the imported descriptors.

### Mutation and versioning

Model objects copy their collection inputs and expose read-only views. To change tile content, construct replacement `TileChunk2D`, `TileLayer2DModel`, and `TileMap2DModel` objects, increment the positive version of changed data, and assign the replacement model to `Model`. Reusing a version for changed tile or atlas data is unsupported because the retained cache uses chunk, tileset, and resource versions to decide whether a batch is current.

Changing only scene presentation properties such as map/layer transforms, opacity, or offsets does not rebuild static tile geometry. A changed chunk rebuilds its dependent cached segments; a changed tileset/resource invalidates only batches that reference it.

### Limits

- Every map has one uniform destination `TileSize`; individual definitions vary their atlas source rectangle, not their destination cell size.
- Tile IDs are global across all tilesets in one model. ID `0` is reserved for an empty cell.
- Static tile flips are horizontal and vertical only. Unknown flag bits are rejected.
- Chunks within one layer may not overlap. A finite map rejects chunks outside its bounds; a null bounds value represents a sparse map, not an eagerly allocated infinite rectangle.
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
| `Layers` | Gets the high-level layer presentation nodes. |
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
