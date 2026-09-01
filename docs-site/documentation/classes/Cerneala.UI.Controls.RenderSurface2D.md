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

Declare retained sprites and templated sprite collections in markup. Cerneala bindings use the `$` path syntax and an explicit mode when required.

```xml
<RenderSurface2D
    ViewBox="$DataContext.BoardBounds:OneWay"
    Stretch="Uniform">
    <RenderSurface2D.Scene>
        <Scene2D>
            <SceneItems2D ItemsSource="$DataContext.LockedPieces:OneWay">
                @templates
                {
                    <ContentTemplate DataType="Game.PieceSpriteModel">
                        <Sprite2D
                            Source="$DataContext.Image:OneWay"
                            SourceRect="$DataContext.SourceRect:OneWay"
                            Destination="$DataContext.Destination:OneWay"
                            Tint="$DataContext.Tint:OneWay" />
                    </ContentTemplate>
                }
            </SceneItems2D>
            <Sprite2D
                Source="$DataContext.CurrentImage:OneWay"
                Destination="$DataContext.CurrentDestination:OneWay"
                IsVisible="$DataContext.HasCurrentPiece:OneWay" />
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

Use on-demand rendering for static or infrequently changing scenes.

```csharp
surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
surface.InvalidateFrame();
```

## Remarks
`RenderSurface2D` follows the `ContentControl` ownership and layout contract. Its game surface is rendered after the inherited background and before the border and retained `Content`, so ordinary Cerneala controls can form an interactive overlay above the game image.

Drawing runs inside the Cerneala frame loop. Cerneala owns the render target, presentation, batch lifetime, and graphics-device state. Application code receives only `RenderSurface2DFrame`, whose operations are limited to 2D primitives and sprites.

`Continuous` redraw mode evaluates the drawing callbacks every Cerneala frame. The backend records the resulting mapped 2D command stream and retains both that stream and the rendered surface. When the stream is visually identical to the previous frame, GPU rasterization is skipped. When commands change, only the affected surface region is cleared and recomposed from the current commands that intersect it, in drawing order. Complex transformed sprites can conservatively invalidate the whole surface.

`OnDemand` redraw mode reuses the last rendered surface without evaluating the callbacks until layout, a relevant property, or `InvalidateFrame()` marks it dirty. Prism images used by the most recently rendered frame are tracked automatically: changing an operation or the live `PrismPipeline` marks the surface dirty without an application-level invalidation call. State used only to calculate manual primitives has no drawable dependency to track and still requires `InvalidateFrame()`.

Prism execution inside the managed surface uses retained result caching. When a surface rasterization is required, unchanged final or intermediate Prism results can be reused instead of executing their passes again. Pipeline mutations invalidate retained results owned by the affected `PrismImage`, while disposing the image forwards deterministic owner invalidation to the surface session. Changing an animated Prism value therefore requires producing that image for its new value, but unchanged Prism images replayed in the same surface can still reuse their retained results.

`ClearColor` initializes the surface and erases damaged regions before their commands are replayed. `OnDraw` records first, followed by `Draw` subscribers in subscription order, followed by `Scene`. Retained `Content` is rendered above the completed surface. The frame object is valid only while the imperative callbacks execute.

`Scene` is an optional logical retained tree. Its nodes reuse Cerneala data context, binding, UI-property, attachment, and invalidation behavior, but they are not added to the visual layout tree. Scene child order is drawing order. Changing a scene-node UI property invalidates the owning surface, including in `OnDemand` mode.

`Sprite2D` scene nodes can own Aspect, Motion, and inline Prism markup. A sprite Prism scope captures only that sprite's image command. Its control bounds are the sprite's `Destination`, while its effective transform includes the scene's `ViewBox` mapping. Prism effects change presentation only; they do not change scene ordering, destination coordinates, or layout.

When `ViewBox` is non-null, it defines the scene's logical coordinate rectangle. `Stretch` maps that rectangle into the surface bounds and the scene is clipped to those bounds. The transform applies only to `Scene`; imperative `OnDraw` and `Draw` commands continue to use local surface pixels. A view box must have positive width and height.

Internally allocated rendering resources, including retained Prism results, are released when the control detaches from its root.

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
