# RenderSurface2D Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2D.cs`

Presentation state: `UI/Controls/RenderSurface2D.Presentation.cs`

Hosts a specialized 2D game-rendering surface behind a retained Cerneala content subtree.

```csharp
public class RenderSurface2D : ContentControl, ITimeSensitiveRenderElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `RenderSurface2D`

## Examples
Draw a game frame without managing graphics-device state, render targets, or batches.

```csharp
RenderSurface2D surface = new()
{
    ClearColor = new Color(8, 11, 17)
};
surface.Draw += (_, frame) =>
{
    frame.FillRectangle(new DrawRect(24, 24, 96, 48), Color.Cyan);
    frame.DrawSprite(player, new DrawRect(160, 80, 32, 32), Color.White);
};
```

Subclass the control when the drawing behavior belongs to a reusable custom control.

```csharp
public sealed class WorldView : RenderSurface2D
{
    public WorldView() => Draw += DrawWorld;

    private void DrawWorld(RenderSurface2D sender, RenderSurface2DFrame frame)
    {
        frame.FillRectangle(frame.Bounds, Color.Black);
    }
}
```

Custom controls register their drawing callback explicitly, using the same `Draw`
event as application code. The former protected `OnDraw` override contract has
been removed: migrate an override to an event handler and subscribe it from the
constructor. There is no runtime member discovery or override cache. Constructor
subscriptions precede subscribers added later; all handlers follow subscription
order. A surface with neither a `Scene` nor a `Draw` subscriber is inactive.

Declare one retained scene root, then nest as many transformed `Scene2D` groups or layers as the world needs. This source-generator-tested example shares one typed atlas, enables stable layer-then-Y ordering, and puts Aspect, Motion, and Prism on a group, a layer, and the sprite produced by a template. Assign or bind `SceneItems2D.ItemsSource` when template instances are needed.

```xml
<RenderSurface2D
    xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
    <RenderSurface2D.Resources>
        <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
    </RenderSurface2D.Resources>
    <RenderSurface2D.Scene>
        <Scene2D OrderMode="LayerThenY"
                 TranslateX="32"
                 TransformOrigin="128,96">
            <Scene2D.Aspect>
                @on Loaded
                {
                    @animate with Tween(100ms)
                    {
                        @to { TranslateY = 8; }
                    }
                }
            </Scene2D.Aspect>
            @prism
            {
                @layer GroupContent
                {
                    Opacity = 1;
                    @filter Blur { Radius = 1; }
                }
            }
            <Scene2D Layer="1">
                <Scene2D.Aspect>
                    @on Loaded
                    {
                        @animate with Tween(100ms)
                        {
                            @to { Opacity = 0.75; }
                        }
                    }
                </Scene2D.Aspect>
                @prism
                {
                    @layer LayerContent
                    {
                        Opacity = 1;
                        @filter Blur { Radius = 1; }
                    }
                }
                <SceneItems2D>
                    @templates
                    {
                        <ContentTemplate DataType="System.String">
                            <Sprite2D Image="$WorldAtlas">
                                <Sprite2D.Aspect>
                                    @on Loaded
                                    {
                                        @animate with Tween(100ms)
                                        {
                                            @to { Opacity = 0.5; }
                                        }
                                    }
                                </Sprite2D.Aspect>
                                @prism
                                {
                                    @layer SpriteContent
                                    {
                                        Opacity = 1;
                                        @filter Blur { Radius = 1; }
                                    }
                                }
                            </Sprite2D>
                        </ContentTemplate>
                    }
                </SceneItems2D>
            </Scene2D>
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

Use on-demand rendering for static or infrequently changing scenes.

```csharp
surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
surface.InvalidateFrame();
```

Convert between input-root coordinates and the logical scene coordinates used by `ViewBox` and scene nodes.

```csharp
using System.Numerics;

if (surface.TryRootToScene(new Vector2(pointerX, pointerY), out Vector2 scenePoint))
{
    Vector2 rootPoint = surface.SceneToRoot(scenePoint);
}
```

## Remarks
`RenderSurface2D` follows the `ContentControl` ownership and layout contract. Its game surface is rendered after the inherited background and before the border and retained `Content`, so ordinary Cerneala controls can form an interactive overlay above the game image.

Drawing runs inside the Cerneala frame loop. Cerneala owns the render target, presentation, batch lifetime, and graphics-device state. Application code receives only `RenderSurface2DFrame`, whose operations are limited to 2D primitives and sprites.

The SDL_GPU backend selects multisampling for the surface independently of the hosting window, including single-sample design-preview windows. It requests eight samples and falls back to four, two, or one according to device support for the surface format. The resolved surface is then composited into the window; applications do not manage its multisample target.

`Continuous` redraw mode evaluates the drawing callbacks every Cerneala frame. The backend records the resulting mapped 2D command stream and retains both that stream and the rendered surface. When the stream is visually identical to the previous frame, GPU rasterization is skipped. When commands change, only the affected surface region is cleared and recomposed from the current commands that intersect it, in drawing order. Complex transformed sprites can conservatively invalidate the whole surface.

For a scene without imperative drawing callbacks, a continuous tick alone does not change its Prism content version. Scene mutations, effective animation changes, and explicit invalidation still change that version; continuous recording remains enabled.

`OnDemand` redraw mode reuses the last rendered surface without evaluating the callbacks until layout, a relevant property, or `InvalidateFrame()` marks it dirty. Prism images used by the most recently rendered frame are tracked automatically: changing an operation or the live `PrismPipeline` marks the surface dirty without an application-level invalidation call. State used only to calculate manual primitives has no drawable dependency to track and still requires `InvalidateFrame()`.

Prism execution inside the managed surface uses retained result caching. When a surface rasterization is required, unchanged final or intermediate Prism results can be reused instead of executing their passes again. Pipeline mutations invalidate retained results owned by the affected `PrismImage`, while disposing the image forwards deterministic owner invalidation to the surface session. Changing an animated Prism value therefore requires producing that image for its new value, but unchanged Prism images replayed in the same surface can still reuse their retained results.

`ClearColor` initializes the surface and erases damaged regions before their commands are replayed. `Draw` subscribers record in subscription order, followed by `Scene`. Retained `Content` is rendered above the completed surface. The frame object is valid only while the imperative callbacks execute.

`Scene` is an optional single logical retained root. A surface cannot hold multiple sibling roots in that property, but the root can contain any number of nested `Scene2D` groups and layers. A reusable component derived from `Scene2D`, such as a paired `<local:HouseView />`, can be that root or a child group; see [Scene2D](Cerneala.UI.Controls.Scene2D.md) for the paired-file contract and example. A `UserControl` is not a scene node. Scene nodes reuse Cerneala data context, binding, UI-property, attachment, Aspect, Motion, Prism, and invalidation behavior, but they are not added to the visual layout tree. Scene child order is drawing order. Changing a scene-node UI property invalidates the owning surface, including in `OnDemand` mode.

Scene input remains UI input. `HitTestService` first tests retained visual children drawn over the surface, then delegates scene geometry to the surface, and finally considers the surface itself. A selected scene node carries its real `UiElementId` and routes inherited events through its scene ancestors, this surface, and the visual UI ancestors. Hover, pressed state, cursor, capture, focus, keyboard, text, commands, `Handled`, and `handledEventsToo` therefore use the existing UI services; there is no game-only event router.

Within the scene, picking uses the enabled geometry of the optional collider owned by each `Sprite2D`, with that owner as the routed target. Each such owner accepts at most one collider. Known visual bounds remain a picking fallback; colliders cannot be declared directly on a scene group or surface. The effective scene drawing order is tested in reverse. Visibility, `IsHitTestVisible`, UI `IsEnabled`, transforms, ViewBox mapping, and the surface clip participate; opacity and Prism do not change the hit geometry. Batch-only tile cells are not individual input elements; use an ordinary `Sprite2D` for individual interaction.

`Scene2D` groups, layers, and `Sprite2D` nodes can own Aspect, Motion, and inline Prism markup. Nodes produced by `SceneItems2D` receive those capabilities from their `@templates` declaration; the materializer does not add a second effect layer. A sprite Prism scope captures only that sprite's image command, while a group scope captures its descendants. Bounds follow the same scene transform used by drawing, including the scene's `ViewBox` mapping. Prism effects change presentation only; they do not change scene ordering, destination coordinates, or layout.

Layer-style spatial parameters, including `OuterGlow.Size` and `BevelEmboss.Size`, keep their catalog DIP units. Scene transforms and `ViewBox` scaling map the captured geometry; they do not multiply those style distances. DPI scaling still applies: at 125% DPI, `OuterGlow.Size = 4` produces a sampling size of 5 pixels regardless of the number of pixels occupied by a scene unit.

When `ViewBox` is non-null, it defines the scene's logical coordinate rectangle. `Stretch` maps that rectangle into the surface bounds and the scene is clipped to those bounds. The transform applies only to `Scene`; imperative `Draw` commands continue to use local surface pixels. A view box must have positive width and height.

`TryRootToScene` and `SceneToRoot` use the exact scene-to-root transform used by rendering: the `ViewBox`/`Stretch` mapping followed by the surface's visual ancestor transforms. `TryRootToScene` returns `false` for non-finite input or a non-invertible transform and sets its output to the default vector. `SceneToRoot` rejects non-finite input with `ArgumentOutOfRangeException`. Mouse handlers can use `MouseEventArgs.GetPosition` for the same conversion relative to the surface, a scene group, or the routed scene node.

Without `ViewBox`, scene coordinates are local surface pixels, not root DIPs. Conversion includes the surface's arranged origin and the mapping from its pixel raster into its logical bounds. SDL_GPU recording and input share the same raster-size calculation, rounding each DPI-scaled extent upward before mapping. With `ViewBox`, its mapping is composed in raster coordinates before the pixels-to-layout and visual transforms; imperative drawing is unchanged.

Internally allocated rendering resources, including retained Prism results, are released when the control detaches from its root.

### Scene preparation and input availability

UI attachment gives `Scene` a common [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md) using this root's existing relay. The surface supplies its viewport and owns retirement when detached or when `Scene` changes; it does not create a second dispatcher. An independent context must be disposed before its root scene can be adopted by a surface. A conflicting owner is rejected before the `Scene` property or either tree changes. Application code must not independently pump or dispose the surface-owned context.

`PresentationState` and `PresentationError` are read-only UI properties for application-composed loading/error UI. The state starts at `Ready` for an empty surface and is refreshed during surface updates, recording, and scene input availability checks. `Loading` means required visible spatial identities or sprite/Prism images have not been prepared; `Error` exposes the preparation failure preventing that coverage. A failure or pending simulation load outside the required visual coverage does not by itself suppress a ready viewport. Neither state blocks the UI relay or changes node visibility, enabled state, animation registration, or collision participation.

While required coverage is unavailable, none of `Scene` is submitted, including already-ready sibling nodes. There is no retained stale-scene fallback, automatic camera movement, or replay of input after recovery. `Draw` remains a separate imperative drawing path, and retained `Content` remains available above the surface; supply the loading/error visuals there. Ordinary exceptions from drawing code are not converted into source-preparation errors.

Scene routes are unavailable while the scene is unready. This includes old input-map snapshots: captured pointer and focused keyboard targets are checked again before lookup, not merely excluded from new hit tests. The ordinary capture/focus services retire an unroutable target on dispatch; recovery does not automatically restore it. Source publication from a worker can invalidate visible coverage before its UI notification is drained. A publication or camera change detected during recording discards the entire scene command tail and its optional preparation, while preserving the preceding imperative commands.

The integration checks `SceneItems2D` payload/template coverage, source-backed `TileMap2D` chunks and atlas images, `Sprite2D` images, and live path-backed images used by scene Prism scopes. Required cold images use the root's shared cache and explicit `IAsyncImageLoader` capability, never a synchronous-loader fallback. A sprite and its Prism mask can share one pending decode while retaining independent acquisitions. Completion requests ordinary UI invalidation; scene command recording and sprite bounds observation use resident images only. Failed unchanged acquisitions do not cause an automatic retry loop. Null or unresolved sprite resource references retain their existing no-image behavior rather than becoming preparation failures.

An active scene Prism composition without supported automatic input selection
also requires a finite [PrismInputDomain](Cerneala.UI.Controls.SceneNode2D.md#finite-prism-input-domains)
on its own owner when the scene contains spatial materializers. Missing domains
are presentation errors. Required coverage includes the complete declared input,
not just the camera: off-camera payloads and images can keep the scene `Loading`.
The final camera clip does not truncate that input before the effect. Backend
raster/allocation failures occur later during rendering and are not converted
into asynchronous `PresentationError` values or degraded visual output. This
includes input-wide filters, the seven non-pointwise styles, wrapped edge modes
and other unclassified filters; nested owners require their own declarations.

`SceneItems2D.Preparation`/`PreparationError` remain available for source-specific work and `Refresh()` performs an explicit source retry. Tile-map catalog selection does not retain unloaded cell or placement arrays; required acquisitions validate their payload before presentation. The fixed `TileMapSource2D.FromModel` adapter still retains its caller-owned model and is not a disk-backed package. Readiness is a current observation, not a lease guaranteeing a later camera, image, effect, or catalog revision.

```csharp
RenderSurface2D surface = new();
string status = surface.PresentationState switch
{
    RenderSurface2DPresentationState.Loading => "Loading scene",
    RenderSurface2DPresentationState.Error => surface.PresentationError?.Message ?? "Scene preparation failed",
    _ => "Ready"
};
```

### Scene animation clock

Attached [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) sources are refreshed during surface frame updates and arrangement, including in `OnDemand` mode. Spatial interest follows the conservative viewport in the materializer's local coordinates, using the same ViewBox/stretch/raster-size mapping as recording. During arrangement, both viewport and effect-input projection use the incoming size, not the previous arranged size. Selection also includes simulated entries and the root collision world's prepared/simulation terrain interests. Supported pointwise scene Prism compositions preserve this viewport interest; covered local filters add their finite sampling neighborhood, and declared non-pointwise domains select their complete input instead. Unclassified effects without a domain report a presentation error, not full-source input. Non-invertible or unknown transforms can still make geometric selection conservative. See [Scene2D's Prism input rules](Cerneala.UI.Controls.Scene2D.md#prism-input-and-streamed-children). Realization is not performed inside `Draw` or scene recording. Asynchronous results publish through the root relay; keep the ordinary update loop running while awaiting a materializer's `Preparation` task or collision-region preparation.

Attached [TileMap2D](Cerneala.UI.Controls.TileMap2D.md) grids participate in the same viewport and collision-interest coordination. Their off-camera adapters are retained only by applicable interests, independently of image selection during recording. Complete in-memory model cells are still application data, not unloaded package chunks. A direct `RecordFrame` probe does not establish the layout/preparation viewport; tests that also query spatial collisions must run arrangement or explicitly prepare their query region.

Optional grid batch preparation runs after the entire scene's required command recording, before the surface recording call returns. All maps in that recording share a 256-cell preparation allowance. Priority rotates between maps across recordings; a map with no eligible work consumes no allowance, and unused cells can serve another map. The rotation advances after the first recipient, so small chunks using a remainder cannot repeatedly deny a larger eligible chunk its full-budget turn. Each map keeps its separate bounded warm-memory charge. Required drawing, animation/simulation, and prepared collision data are not clipped by this allowance.

If the camera changes between spatial update and recording, including from a `Draw` callback, already-resident scene coverage can still be drawn at the new camera. Tile-map warm retention and preparation continue to use the same coordinated viewport as required data ownership; optional preparation for the changed viewport waits for the next spatial update. This does not move required payload acquisition or scene-template realization into `Draw` or scene recording, or evict a prepared chunk using a different camera from its owning interests.

Preparation requests exist only for the current recording. A failed required recording cancels its optional work; model publication, cache release, detach, or a newer recording supersedes pending work for that map. The surface retains only a priority index between recordings, not queued map or image ownership. Optional preparation does not itself invalidate an `OnDemand` surface or start a timer: further preparation waits for another ordinary recording. Large jumps may therefore need required synchronous batch construction rather than already-prepared batches.

Attached sprite animations advance from the existing UI frame delta, including in `OnDemand` mode. The surface aggregates active instances; it invalidates once when one or more effective frame rectangles or flips change. A delta that leaves presentation unchanged does not invalidate an on-demand surface. Static, paused, zero-rate, and finished non-loop animations do not request time. `Continuous` still invalidates on every UI frame while drawing is active.

Detach removes active registrations and preserves playback positions; reattach resumes them. Hidden or offscreen instances keep advancing while attached. An invisible sprite emits no draw command. Offscreen sprite inputs are culled when their known bounds miss the viewport, except when a sprite or scene-ancestor Prism scope may extend their visual influence. No animation owns a timer or thread. Explicit `ITimeSensitiveRenderElement.UpdateRenderTime` calls reject negative deltas.

## Constructors
| Name | Description |
| --- | --- |
| `RenderSurface2D()` | Initializes an inactive content host; assigning `Scene` or subscribing to `Draw` activates drawing. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `ClearColorProperty` | `UiProperty<Color>` | Identifies the color used to initialize the surface and clear damaged regions. |
| `RedrawModeProperty` | `UiProperty<RenderSurface2DRedrawMode>` | Identifies the frame scheduling mode. |
| `SceneProperty` | `UiProperty<Scene2D?>` | Identifies the retained 2D scene. |
| `ViewBoxProperty` | `UiProperty<DrawRect?>` | Identifies the optional logical scene coordinate rectangle. |
| `StretchProperty` | `UiProperty<DrawBrushStretch>` | Identifies how the view box maps into the surface bounds. |
| `PresentationStateProperty` | `UiProperty<RenderSurface2DPresentationState>` | Identifies the read-only current scene preparation state. |
| `PresentationErrorProperty` | `UiProperty<Exception?>` | Identifies the read-only preparation failure preventing visible scene coverage. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `ClearColor` | `Color` | Gets or sets the color used to initialize the surface and erase damaged regions. |
| `RedrawMode` | `RenderSurface2DRedrawMode` | Gets or sets whether the surface redraws continuously or only when dirty. |
| `Scene` | `Scene2D?` | Gets or sets the optional retained 2D scene recorded after imperative drawing. |
| `ViewBox` | `DrawRect?` | Gets or sets the logical coordinate rectangle applied to `Scene` only. |
| `Stretch` | `DrawBrushStretch` | Gets or sets how `ViewBox` is mapped into the surface bounds. |
| `Content` | `object?` | Gets or sets the retained content rendered above the game surface. Inherited from `ContentControl`. |
| `PresentationState` | `RenderSurface2DPresentationState` | Gets the latest observed scene preparation state; does not stop simulation. |
| `PresentationError` | `Exception?` | Gets the relevant preparation failure, or null for loading/ready coverage. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `InvalidateFrame()` | `void` | Marks the current game surface dirty and schedules a retained render pass. |
| `SceneToRoot(Vector2)` | `Vector2` | Converts a finite logical scene position through the render/ViewBox and visual transforms into input-root coordinates. |
| `TryRootToScene(Vector2, out Vector2)` | `bool` | Attempts to invert the render/ViewBox and visual transforms for an input-root position. |

## Events
| Name | Type | Description |
| --- | --- | --- |
| `Draw` | `RenderSurface2DDrawEventHandler` | Raised when the game surface needs to be redrawn. |

## Property Information
| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `ClearColor` | `ClearColorProperty` | `Color.Transparent` | `AffectsRender` |
| `RedrawMode` | `RedrawModeProperty` | `Continuous` | `AffectsRender` |
| `Scene` | `SceneProperty` | `null` | `AffectsRender` |
| `ViewBox` | `ViewBoxProperty` | `null` | `AffectsRender`; non-null values require positive width and height. |
| `Stretch` | `StretchProperty` | `DrawBrushStretch.Fill` | `AffectsRender` |
| `PresentationState` | `PresentationStateProperty` | `Ready` | Read-only; `None`. State changes invalidate presentation and input-route availability. |
| `PresentationError` | `PresentationErrorProperty` | `null` | Read-only; `None`. |

## Applies To
Project: `Cerneala`

Backend: SDL_GPU retained rendering.

## See Also
- `ContentControl`
- `RenderSurface2DFrame`
- `RenderSurface2DRedrawMode`
- [RenderSurface2DPresentationState](Cerneala.UI.Controls.RenderSurface2DPresentationState.md)
- `RenderSurface2DDrawEventHandler`
- `Scene2D`
- `SceneItems2D`
- `Sprite2D`
- `MouseEventArgs`
- `Collider2D`
- `CollisionWorld2D`
