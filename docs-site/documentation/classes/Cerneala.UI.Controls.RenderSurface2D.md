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

Use on-demand rendering for static or infrequently changing scenes.

```csharp
surface.RedrawMode = RenderSurface2DRedrawMode.OnDemand;
surface.InvalidateFrame();
```

## Remarks
`RenderSurface2D` follows the `ContentControl` ownership and layout contract. Its game surface is rendered after the inherited background and before the border and retained `Content`, so ordinary Cerneala controls can form an interactive overlay above the game image.

Drawing runs inside the Cerneala frame loop. Cerneala owns the render target, presentation, batch lifetime, and graphics-device state. Application code receives only `RenderSurface2DFrame`, whose operations are limited to 2D primitives and sprites.

`Continuous` redraw mode evaluates the drawing callbacks every Cerneala frame. The backend records the resulting mapped 2D command stream and retains both that stream and the rendered surface. When the stream is visually identical to the previous frame, GPU rasterization is skipped. When commands change, only the affected surface region is cleared and recomposed from the current commands that intersect it, in drawing order. Complex transformed sprites can conservatively invalidate the whole surface.

`OnDemand` redraw mode reuses the last rendered surface without evaluating the callbacks until layout, a relevant property, or `InvalidateFrame()` marks it dirty.

`ClearColor` initializes the surface and erases damaged regions before their commands are replayed. `OnDraw` records first, followed by `Draw` subscribers in subscription order. The frame object is valid only while those callbacks execute.

Internally allocated rendering resources are released when the control detaches from its root.

## Constructors
| Name | Description |
| --- | --- |
| `RenderSurface2D()` | Initializes a content host and detects whether its runtime type overrides `OnDraw`. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `ClearColorProperty` | `UiProperty<Color>` | Identifies the color used to initialize the surface and clear damaged regions. |
| `RedrawModeProperty` | `UiProperty<RenderSurface2DRedrawMode>` | Identifies the frame scheduling mode. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `ClearColor` | `Color` | Gets or sets the color used to initialize the surface and erase damaged regions. |
| `RedrawMode` | `RenderSurface2DRedrawMode` | Gets or sets whether the surface redraws continuously or only when dirty. |
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

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `ContentControl`
- `RenderSurface2DFrame`
- `RenderSurface2DRedrawMode`
- `RenderSurface2DDrawEventHandler`
