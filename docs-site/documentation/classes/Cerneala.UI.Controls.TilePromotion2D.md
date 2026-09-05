# TilePromotion2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDocument.cs`

Identifies a sparse candidate for explicit tile promotion while retaining template/gameplay metadata.

```csharp
public sealed class TilePromotion2D
```

## Examples

```csharp
var candidate = new TilePromotion2D(
    new TileCellKey2D("Doors", 2, 3),
    properties: new Dictionary<string, object?> { ["InitialState"] = "Closed" });
```

## Remarks

Construction requires a nonempty layer identity and an optional positive override ID. Scene2DLevel validates that the cell exists, the resolved tile ID is defined, and no address is duplicated. An existing empty cell needs an explicit override.

This is data, not TileInstance2D. Composition calls `TileMap2D.Promote` or declares a `TileInstance2D` under the matching `TileLayer2D`, and supplies any input/collider/Aspect/Motion/Prism behavior. No automatic promotion occurs during validation or import.

### Imported interactive door

The compiled [Scene World sample](../../../Playground/Cerneala.Playground/SceneWorldShowcase.crn)
imports the same village from Tiled and LDtk at load time. Its composition checks
the promotion address `(layer "2", x 14, y 9)` before binding the imported map.
`InitialState` initializes the sample's `DoorClosed`/`DoorState` view-model
properties; the importer does not interpret those values as UI instructions.

This fragment is from that composition. It requires the sample's typed
`SceneWorldState` data context, declared `DoorAnimations` resource and `OnDoor`
handler; it is not a standalone map or a generic entity factory.

```xml
<TileLayer2D LayerId="2">
  <TileInstance2D X="14" Y="9" MouseDown="OnDoor"
                  ReplacesImportedColliders="true"
                  Animations="$DoorAnimations"
                  AnimationState="$DataContext.DoorState:OneWay">
    <BoxCollider2D Width="16" Height="16"
                   CollisionLayer="2" CollisionMask="1"
                   Enabled="$DataContext.DoorClosed:OneWay" />
    <TileInstance2D.Aspect>
      @on MouseDown {
        @animate with Tween(180ms) {
          @from { Opacity = 0.65; }
          @to { Opacity = 1; }
        }
      }
    </TileInstance2D.Aspect>
    @prism {
      @layer DoorGlow {
        @style OuterGlow { Size = 1; Opacity = 0.3; Color = #FFEAC777; }
      }
    }
  </TileInstance2D>
</TileLayer2D>
```

The handler toggles the sample state. The binding enables the collider only
while closed; opening the door does not disable the node's own input. The
handler leaves MouseDown unhandled so the declared Motion trigger can receive
the same routed event. Ordinary cells stay in static batches, while the one
promoted cell is removed from its static slot and drawn once as a node.

Dynamic NPCs use `SceneItems2D` in the same sample. Their Aspect/Motion/Prism
declarations belong on the sprite inside `@templates`, not on the items
container. Image resources are registered by composition through the existing
resource cache; the importer never decodes or uploads an atlas.

## Constructors

| Name | Description |
| --- | --- |
| `TilePromotion2D(TileCellKey2D cell, int? tileId = null, IReadOnlyDictionary<string, object?>? properties = null)` | Copies the optional metadata and validates the address identity/override; owning-level validation resolves the cell. |

## Properties

| Name | Description |
| --- | --- |
| `Cell` | Stable layer/coordinate address. |
| `TileId` | Optional positive replacement tile ID. |
| `Properties` | Shallow copied source properties for composition/templates. |

## See also

- [TileInstance2D](Cerneala.UI.Controls.TileInstance2D.md)
- [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md)
- [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md)
