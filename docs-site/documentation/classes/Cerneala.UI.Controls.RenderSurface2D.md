# RenderSurface2D Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2D.cs`

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
    protected override void OnDraw(RenderSurface2DFrame frame)
    {
        frame.DrawSprite(world, frame.Bounds, Color.White);
    }
}
```

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
                            <Sprite2D SourceResourceId="$WorldAtlas">
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

`Continuous` redraw mode evaluates the drawing callbacks every Cerneala frame. The backend records the resulting mapped 2D command stream and retains both that stream and the rendered surface. When the stream is visually identical to the previous frame, GPU rasterization is skipped. When commands change, only the affected surface region is cleared and recomposed from the current commands that intersect it, in drawing order. Complex transformed sprites can conservatively invalidate the whole surface.

`OnDemand` redraw mode reuses the last rendered surface without evaluating the callbacks until layout, a relevant property, or `InvalidateFrame()` marks it dirty. Prism images used by the most recently rendered frame are tracked automatically: changing an operation or the live `PrismPipeline` marks the surface dirty without an application-level invalidation call. State used only to calculate manual primitives has no drawable dependency to track and still requires `InvalidateFrame()`.

Prism execution inside the managed surface uses retained result caching. When a surface rasterization is required, unchanged final or intermediate Prism results can be reused instead of executing their passes again. Pipeline mutations invalidate retained results owned by the affected `PrismImage`, while disposing the image forwards deterministic owner invalidation to the surface session. Changing an animated Prism value therefore requires producing that image for its new value, but unchanged Prism images replayed in the same surface can still reuse their retained results.

`ClearColor` initializes the surface and erases damaged regions before their commands are replayed. `OnDraw` records first, followed by `Draw` subscribers in subscription order, followed by `Scene`. Retained `Content` is rendered above the completed surface. The frame object is valid only while the imperative callbacks execute.

`Scene` is an optional single logical retained root. A surface cannot hold multiple sibling roots in that property, but the root can contain any number of nested `Scene2D` groups and layers. Scene nodes reuse Cerneala data context, binding, UI-property, attachment, Aspect, Motion, Prism, and invalidation behavior, but they are not added to the visual layout tree. Scene child order is drawing order. Changing a scene-node UI property invalidates the owning surface, including in `OnDemand` mode.

Scene input remains UI input. `HitTestService` first tests retained visual children drawn over the surface, then delegates scene geometry to the surface, and finally considers the surface itself. A selected scene node carries its real `UiElementId` and routes inherited events through its scene ancestors, this surface, and the visual UI ancestors. Hover, pressed state, cursor, capture, focus, keyboard, text, commands, `Handled`, and `handledEventsToo` therefore use the existing UI services; there is no game-only event router.

Within the scene, picking uses collider geometry when an entity declares direct colliders and otherwise uses exact known visual bounds. The effective scene drawing order is tested in reverse. Visibility, `IsHitTestVisible`, UI `IsEnabled`, transforms, ViewBox mapping, and the surface clip participate; opacity and Prism do not change the hit geometry. Batch-only tile cells are not input elements, while promoted `TileInstance2D` nodes are.

`Scene2D` groups, layers, and `Sprite2D` nodes can own Aspect, Motion, and inline Prism markup. Nodes produced by `SceneItems2D` receive those capabilities from their `@templates` declaration; the materializer does not add a second effect layer. A sprite Prism scope captures only that sprite's image command, while a group scope captures its descendants. Bounds follow the same scene transform used by drawing, including the scene's `ViewBox` mapping. Prism effects change presentation only; they do not change scene ordering, destination coordinates, or layout.

When `ViewBox` is non-null, it defines the scene's logical coordinate rectangle. `Stretch` maps that rectangle into the surface bounds and the scene is clipped to those bounds. The transform applies only to `Scene`; imperative `OnDraw` and `Draw` commands continue to use local surface pixels. A view box must have positive width and height.

`TryRootToScene` and `SceneToRoot` use the exact scene-to-root transform used by rendering: the `ViewBox`/`Stretch` mapping followed by the surface's visual ancestor transforms. `TryRootToScene` returns `false` for non-finite input or a non-invertible transform and sets its output to the default vector. `SceneToRoot` rejects non-finite input with `ArgumentOutOfRangeException`. Mouse handlers can use `MouseEventArgs.GetPosition` for the same conversion relative to the surface, a scene group, or the routed scene node.

Without `ViewBox`, scene coordinates are local surface pixels, not root DIPs. Conversion includes the surface's arranged origin and the mapping from its pixel raster into its logical bounds. SDL_GPU recording and input share the same raster-size calculation, rounding each DPI-scaled extent upward before mapping. With `ViewBox`, its mapping is composed in raster coordinates before the pixels-to-layout and visual transforms; imperative drawing is unchanged.

Internally allocated rendering resources, including retained Prism results, are released when the control detaches from its root.

### Scene animation clock

Attached sprite and promoted-tile animations advance from the existing UI frame delta, including in `OnDemand` mode. The surface aggregates active instances; it invalidates once when one or more effective frame rectangles or flips change. A delta that leaves presentation unchanged does not invalidate an on-demand surface. Static, paused, zero-rate, and finished non-loop animations do not request time. `Continuous` still invalidates on every UI frame while drawing is active.

Detach removes active registrations and preserves playback positions; reattach resumes them. Hidden or offscreen instances keep advancing while attached. An invisible sprite emits no draw command. Offscreen sprite inputs are culled when their known bounds miss the viewport, except when a sprite or scene-ancestor Prism scope may extend their visual influence. No animation owns a timer or thread. Explicit `ITimeSensitiveRenderElement.UpdateRenderTime` calls reject negative deltas.

## Constructors
| Name | Description |
| --- | --- |
| `RenderSurface2D()` | Initializes a content host and detects whether its runtime type overrides `OnDraw`. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `ClearColorProperty` | `UiProperty<Color>` | Identifies the color used to initialize the surface and clear damaged regions. |
| `RedrawModeProperty` | `UiProperty<RenderSurface2DRedrawMode>` | Identifies the frame scheduling mode. |
| `SceneProperty` | `UiProperty<Scene2D?>` | Identifies the retained 2D scene. |
| `ViewBoxProperty` | `UiProperty<DrawRect?>` | Identifies the optional logical scene coordinate rectangle. |
| `StretchProperty` | `UiProperty<DrawBrushStretch>` | Identifies how the view box maps into the surface bounds. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `ClearColor` | `Color` | Gets or sets the color used to initialize the surface and erase damaged regions. |
| `RedrawMode` | `RenderSurface2DRedrawMode` | Gets or sets whether the surface redraws continuously or only when dirty. |
| `Scene` | `Scene2D?` | Gets or sets the optional retained 2D scene recorded after imperative drawing. |
| `ViewBox` | `DrawRect?` | Gets or sets the logical coordinate rectangle applied to `Scene` only. |
| `Stretch` | `DrawBrushStretch` | Gets or sets how `ViewBox` is mapped into the surface bounds. |
| `Content` | `object?` | Gets or sets the retained content rendered above the game surface. Inherited from `ContentControl`. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `InvalidateFrame()` | `void` | Marks the current game surface dirty and schedules a retained render pass. |
| `OnDraw(RenderSurface2DFrame)` | `void` | Draws a frame in a derived control before event subscribers execute. |
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

## Applies To
Project: `Cerneala`

Backends: SDL_GPU and MonoGame/WindowsDX retained rendering.

## See Also
- `ContentControl`
- `RenderSurface2DFrame`
- `RenderSurface2DRedrawMode`
- `RenderSurface2DDrawEventHandler`
- `Scene2D`
- `SceneItems2D`
- `Sprite2D`
- `MouseEventArgs`
- `Collider2D`
- `CollisionWorld2D`
