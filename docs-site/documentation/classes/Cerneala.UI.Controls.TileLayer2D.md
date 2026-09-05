# TileLayer2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileLayer2D.cs`

Provides the addressable scene presentation for one `TileLayer2DModel`.

```csharp
public sealed class TileLayer2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `TileLayer2D`

## Examples

```xml
<TileLayer2D LayerId="Buildings" Tint="#FFE8CC" TranslateX="4">
  <TileLayer2D.Aspect>
    @on Loaded
    {
      @animate with Tween(100ms)
      {
        @to { Opacity = 0.8; }
      }
    }
  </TileLayer2D.Aspect>
  @prism
  {
    @layer BuildingsEffect
    {
      Opacity = 1;
      @filter Blur { Radius = 1; }
    }
  }
  <TileInstance2D X="18" Y="11" />
</TileLayer2D>
```

## Remarks

`LayerId` must match exactly one layer in the owning map model. When a model is assigned, `TileMap2D` creates presentation nodes for model layers that have no explicit declaration. An explicit declaration with an empty or unknown ID is rejected during layer synchronization, as are duplicate presentation IDs.

The model and presentation values compose rather than replace one another: offsets are added, opacity is multiplied, and tint channels are multiplied. The inherited scene transform is then applied around `TransformOrigin`. Visibility from either layer representation can suppress recording. Aspect and Motion can supply or animate the presentation `UiProperty` values; Prism captures only this layer's commands.

Internal tile-layer order comes from `TileLayer2DModel.Order` and model source order. `TileMap2D` does not consult the presentation layer's inherited `SceneNode2D.Layer` value. To order the whole map among siblings, set `TileMap2D.Layer` and use a sorting `Scene2D`; neither property replaces the model's per-layer order.

`PromotedTiles` is sparse and is the only per-cell node collection owned by the layer. Each `(X,Y)` coordinate must be unique in the collection and must belong to an existing model chunk. Inserting, replacing, removing, or clearing an instance updates the logical scene tree and surface attachment. Removing an instance restores the corresponding static cell on the next recording.

## Properties

| Name | Description |
| --- | --- |
| `LayerId` | Stable model-layer identifier. |
| `Offset` | Additional scene-space offset composed with the model offset. |
| `Tint` | Tint multiplied with the model tint and promoted-tile tint. |
| `TransformOrigin` | Origin for inherited transform properties. |
| `PromotedTiles` | Sparse collection of individually addressable tile nodes. |

## Applies to

Project: `Cerneala`

## See also

- [TileLayer2DModel](Cerneala.UI.Controls.TileLayer2DModel.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [TileInstance2D](Cerneala.UI.Controls.TileInstance2D.md)
