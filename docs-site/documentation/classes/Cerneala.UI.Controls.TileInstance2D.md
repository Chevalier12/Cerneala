# TileInstance2D Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileInstance2D.cs`

Represents one explicitly promoted tile cell with its own scene-node lifecycle.

```csharp
public sealed class TileInstance2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `TileInstance2D`

## Examples

```xml
<TileInstance2D X="18"
                Y="11"
                Tint="White"
                ReplacesImportedColliders="true">
  <BoxCollider2D Width="16"
                 Height="4"
                 OffsetY="12"
                 CollisionLayer="2"
                 CollisionMask="1" />
  <TileInstance2D.Aspect>
    @on Loaded
    {
      @animate with Tween(100ms)
      {
        @to { Opacity = 0.8; }
      }
    }
  </TileInstance2D.Aspect>
</TileInstance2D>
```

## Remarks

`X` and `Y` are tile coordinates in the owning layer. They identify a real cell, not an arbitrary scene position. Coordinates must remain unique among the layer's promoted instances and must resolve to an existing chunk. Invalid runtime changes are rejected when the map validates the promoted collection for recording.

Null visual overrides inherit the static cell definition: `TileId` inherits the cell ID, `SourceRect` inherits the resolved definition rectangle, and `Flip` inherits the cell flip. `Tint` multiplies with the map layer's model and presentation tints. An empty cell must have a positive `TileId` override. The override must resolve through one of the owning model's tilesets.

While the node is promoted, the owning map suppresses the same static cell so it is drawn exactly once in its row-major semantic slot. The node has its own inherited transform, opacity, visibility, Aspect, Motion, Prism, attachment, and UI event state. Per-instance transform and Prism bounds are one destination tile in local coordinates before the inherited transform is applied.

A promoted instance can be the target for application behavior without turning every map cell into a `UIElement`. `Colliders` is its content property, so collider elements can be written directly inside the instance with Cerneala `.crn` syntax. These are real `Collider2D` scene nodes: they inherit the promoted tile's data context and transform, enter the owning scene's collision world, and use the same mutation path as other colliders.

The promoted instance and its explicit colliders participate in the common scene input route and geometric picking rules. A batch-only cell remains data and cannot be a routed-event target; promote only cells that need per-instance input, state, Motion, or Prism behavior.

`ReplacesImportedColliders="false"`, the default, composes explicit colliders with descriptors imported by the tile definition. Set it to `true` when the explicit collection is the complete collision representation for that promoted cell. Replacement suppresses only the imported colliders for that cell; demotion restores them. This explicit policy prevents an interactive door or other promoted tile from accidentally receiving the same collider twice.

Promotion can introduce a static batch split when the instance has static cells before and after it in the same order segment. Individual Prism adds an isolated effect scope around the instance. For decorative cells that need no per-cell state, leave them in the static model.

## Sprite-sheet animation

A promoted tile consumes the same immutable [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md), frame sampler, and state-selection contract as [Sprite2D](Cerneala.UI.Controls.Sprite2D.md). It does not create another animation engine. Each instance owns its progress; the set can be shared with sprites and other promoted tiles.

The current frame overrides the effective source rectangle. Its flip composes by XOR with the resolved tile flip (instance override or static cell flip). Animation does not overwrite the static fallback properties. Replacing the definition set resets progress; changing only data context does not. `Restart` and `Resume` govern state switches, and `RestartAnimation()` resets the selected clip. Pause or rate zero preserve progress.

Playback uses the same UI-frame delta and active registration as sprites. Hidden or culled attached instances keep progressing without drawing; detach removes their time request while preserving progress, and reattach resumes. Pause, rate zero, and non-loop completion stop the active request. On-demand invalidation is aggregated by the surface, and frame changes do not rebuild neighboring static batches.

Declare the set in an ancestor's `<OwnerType.Resources>` and reference it on the promoted instance:

```xml
<TileInstance2D X="2" Y="3"
                Animations="$HeroAnimations"
                AnimationState="Walk"
                AnimationStateChangeMode="Resume" />
```

The owning model must still contain the addressed cell and resolve its tile image. Animation never restores the promoted cell to the static batch: it remains drawn once in its semantic slot, with neighboring ordinary cells batched. Individual Prism processes the current animated frame within the tile's existing bounds and does not change collision geometry.

For example, this promoted-cell fragment combines the shared state with an Aspect-triggered Motion and an individual Prism. Place it in the appropriate `TileLayer2D`; `HeroAnimations` is the ancestor resource shown on [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md).

```xml
<TileInstance2D X="2" Y="3" Animations="$HeroAnimations" AnimationState="Idle">
  <TileInstance2D.Aspect>
    @on Loaded
    {
      @animate with Tween(100ms)
      {
        @to { Opacity = 0.8; TranslateY = 2; }
      }
    }
  </TileInstance2D.Aspect>
  @prism
  {
    @layer TileContent { Opacity = 1; @filter Blur { Radius = 1; } }
  }
</TileInstance2D>
```

Aspect can select definitions/state and set pause/mode. Motion can interpolate playback rate and supported visual properties, not the clip collection or discrete frame/state selection. The five animation properties below have public `<Name>Property` identifier fields and `AffectsRender` metadata.

## Properties

| Name | Description |
| --- | --- |
| `X`, `Y` | Stable tile coordinates. |
| `TileId` | Optional positive replacement tile ID. |
| `SourceRect` | Optional atlas source-rectangle override. |
| `Tint` | Per-instance tint. |
| `Flip` | Optional per-instance horizontal/vertical/diagonal flip override. Diagonal swaps normalized axes before the other flags. |
| `Animations` | Shared `SpriteAnimationSet?`; defaults to `null`. |
| `AnimationState` | Case-sensitive `string?` clip name; defaults to `null`. Missing runtime states use static fallback visuals. |
| `AnimationPlaybackRate` | Finite nonnegative `double`; defaults to `1`. Zero preserves progress. |
| `IsAnimationPaused` | `bool`; defaults to `false`. True preserves progress. |
| `AnimationStateChangeMode` | `SpriteAnimationStateChangeMode`; defaults to `Restart`. `Resume` restores saved progress for a state. |
| `TransformOrigin` | Origin for inherited transform properties. |
| `Colliders` | Mutable collection of declarative `Collider2D` children; this is the markup content property. |
| `ReplacesImportedColliders` | Whether explicit colliders suppress the imported descriptors for the promoted cell. The default is `false`. |

## Methods

| Name | Description |
| --- | --- |
| `RestartAnimation()` | Resets the selected clip to frame zero; invalidates its surface if the visual frame changes. |

## Applies to

Project: `Cerneala`

## See also

- [TileCellKey2D](Cerneala.UI.Controls.TileCellKey2D.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [TileLayer2D](Cerneala.UI.Controls.TileLayer2D.md)
- [TileColliderDescriptor2D](Cerneala.UI.Controls.TileColliderDescriptor2D.md)
- [Collider2D](Cerneala.UI.Controls.Collider2D.md)
