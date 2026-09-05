# Scene2DDebugOverlay Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDebugOverlay.cs`

Draws presentation-only diagnostics for its containing scene subtree.

```csharp
public sealed class Scene2DDebugOverlay : SceneNode2D
```

## Examples

Add the overlay directly to `Scene2D.Children`:

```csharp
Scene2DDebugOverlay debug = new()
{
    Flags = Scene2DDebugFlags.Colliders | Scene2DDebugFlags.PromotedTiles,
    LineThickness = 1
};
scene.Children.Add(debug);
debug.Flags = Scene2DDebugFlags.None;
```

Real Cerneala markup can attach presentation effects:

```xml
<Scene2DDebugOverlay Flags="All" LineThickness="1">
  <Scene2DDebugOverlay.Aspect>
    @on Loaded
    {
      @animate with Tween(100ms)
      {
        @to { Opacity = 0.75; LineThickness = 2; }
      }
    }
  </Scene2DDebugOverlay.Aspect>
  @prism
  {
    @layer DebugPresentation
    {
      @filter Blur { Radius = 1; }
    }
  }
</Scene2DDebugOverlay>
```

## Remarks

The overlay is recorded after its containing scene's gameplay children, outside their order list. `Layer` does not reposition the debug pass. It has no picking bounds, is excluded from scene input routing even if `IsHitTestVisible` is assigned, and does not contribute to a group's gameplay bounds/Y anchor. Imported documents and models never contain this presentation node.

The containing scene's transforms and surface clip apply normally. Overlay transforms move only diagnostic ink; they do not move the observed geometry. `LineThickness` and `FontSize` are local scene-space values, so camera zoom and affine transforms scale them. Outlines use centered vector strokes, including fractional thickness; they do not use the legacy pixel-quantized rectangle outline. Labels use Consolas through the existing font resolver. Prism encloses only the debug pass, with the conservative visible local viewport as its bounds. A singular presentation transform produces no diagnostic ink.

Flags default off. An off overlay records no commands and does not query maps, collision geometry, or navigation data. The direct recording path is allocation-free after warmup; this is not a zero-allocation claim for the entire scene renderer.

Collider diagnostics query the existing collision broadphase without running contact resolution or adding gameplay-query counters. Only active indexed colliders are shown. Solid collider colors are deterministically derived from collision-layer bits; triggers are orange and zero-mask colliders gray. Labels contain exact hexadecimal layer/mask values and `solid`, `trigger`, or `masked`. These are participation/filter states, not a claim that two shapes currently touch: collision queries are stateless and the overlay does not invent a contact history.

Map diagnostics reuse the chunk spatial index and inspect only viewport-intersecting cell ranges. Sparse promoted instances are inspected independently so a moved instance can be related to its original batch slot. Cyan outlines identify chunks; magenta outlines identify a promoted slot and current transformed quad, joined at their centers. Order labels observe the recorded gameplay order instead of re-sorting with the debug transform.

The external navigation grid is queried only for viewport-intersecting coordinates. Green means traversable, red blocked, and a missing cell has no outline. No pathfinding or implied collision is performed. If the provider changes internally, request another frame through the existing `RenderSurface2D.InvalidateFrame()` API; assigning a new provider invalidates rendering automatically.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DDebugOverlay()` | Creates a disabled, non-hit-testable overlay. |

## Properties and UiProperty fields

| Property | Identifier | Default | Contract |
| --- | --- | --- | --- |
| `Flags` | `FlagsProperty` | `None` | Independent defined bits only; affects rendering. |
| `LineThickness` | `LineThicknessProperty` | `1` | Positive finite `float`; affects rendering. |
| `FontSize` | `FontSizeProperty` | `10` | Positive finite `float`; affects rendering. |
| `NavigationGrid` | `NavigationGridProperty` | `null` | External read-only data provider; affects rendering. |

These properties participate in the normal Aspect/binding system. Numeric properties support Motion. Inherited opacity and scene transforms affect only debug presentation.

## Methods

| Name | Description |
| --- | --- |
| `GetDiagnosticsSnapshot()` | Returns counters from the last overlay recording; counters reset on each recording, including when disabled. |

## See also

- [Scene2DDebugFlags](Cerneala.UI.Controls.Scene2DDebugFlags.md)
- [IScene2DDebugNavigationGrid](Cerneala.UI.Controls.IScene2DDebugNavigationGrid.md)
- [Scene2DDebugOverlayDiagnostics](Cerneala.UI.Controls.Scene2DDebugOverlayDiagnostics.md)
