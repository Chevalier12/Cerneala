# RenderSurface2D Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2D.cs`

Hosts a MonoGame 2D render surface behind a retained Cerneala content subtree.

```csharp
public class RenderSurface2D : ContentControl, ITimeSensitiveRenderElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `RenderSurface2D`

## Examples
Use the managed event mode when Cerneala should own the render target and frame scheduling. `Begin` accepts the complete `SpriteBatch.Begin` configuration.

```csharp
RenderSurface2D surface = new();
surface.DrawSurface += context =>
{
    context.Begin(
        SpriteSortMode.Immediate,
        BlendState.AlphaBlend,
        SamplerState.PointClamp,
        DepthStencilState.None,
        RasterizerState.CullNone,
        effect: null,
        transformMatrix: Matrix.Identity);
    context.SpriteBatch.Draw(playerTexture, context.Bounds, Color.White);
    context.End();
};
```

Present an existing texture manually when another system owns the render target.

```csharp
surface.Present(gameRenderTarget);
```

Subclass the control when the drawing behavior belongs to a reusable custom control.

```csharp
public sealed class WorldView : RenderSurface2D
{
    protected override void OnDrawSurface(RenderSurface2DDrawContext context)
    {
        context.Begin(samplerState: SamplerState.PointClamp);
        context.SpriteBatch.Draw(worldTexture, context.Bounds, Color.White);
        context.End();
    }
}
```

## Remarks
`RenderSurface2D` follows the `ContentControl` ownership and layout contract. Its game surface is rendered after the inherited background and before the border and retained `Content`, so ordinary Cerneala controls can form an interactive overlay above the game image.

The control supports three presentation modes:

1. `Present(Texture2D?)` displays a texture owned by the caller.
2. `DrawSurface` asks Cerneala to allocate and refresh an offscreen render target before invoking subscribers.
3. Overriding `OnDrawSurface(RenderSurface2DDrawContext)` provides the same managed rendering path for reusable derived controls.

Managed callbacks run inside the Cerneala frame loop. The user does not own another game loop or call `Present` on the graphics device. Cerneala suspends its UI batch, isolates the graphics-device state, invokes the surface drawing code, restores the host state, and then continues retained UI rendering.

Event and override modes redraw automatically on every Cerneala frame. Manual mode redraws whenever `Surface` changes or `Present(Texture2D?)` is called, including repeated calls with the same texture instance after its pixels have changed.

`RenderSurface2DDrawContext.Begin` mirrors every parameter accepted by MonoGame's `SpriteBatch.Begin`. A callback may use multiple begin/end passes, custom effects, custom blend/depth/rasterizer/sampler states, or direct `GraphicsDevice` operations. Cerneala automatically ends a batch started through the context if the callback leaves it open.

The manual `Surface` texture remains owned by the caller and is never disposed by the control. Internally allocated managed resources are released when managed mode ends or the control detaches from its root.

## Constructors
| Name | Description |
| --- | --- |
| `RenderSurface2D()` | Initializes a content host and detects whether its runtime type overrides `OnDrawSurface`. |

## Fields
| Name | Type | Description |
| --- | --- | --- |
| `SurfaceProperty` | `UiProperty<Texture2D?>` | Identifies the caller-owned manual surface property. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Surface` | `Texture2D?` | Gets or sets the caller-owned texture used by manual presentation mode. |
| `Content` | `object?` | Gets or sets the retained content rendered above the game surface. Inherited from `ContentControl`. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `Present(Texture2D?)` | `void` | Selects a caller-owned texture for manual presentation, or clears it when passed `null`. |
| `RefreshSurface()` | `void` | Invalidates the surface and marks managed content dirty. |
| `ClearSurface()` | `void` | Clears the manual surface. |
| `UpdateRenderTime(TimeSpan)` | `bool` | Requests the next managed redraw from the Cerneala frame loop. |
| `OnDrawSurface(RenderSurface2DDrawContext)` | `void` | Draws a managed surface in a derived control. |

## Events
| Name | Type | Description |
| --- | --- | --- |
| `DrawSurface` | `RenderSurface2DDrawEventHandler` | Raised when a managed surface needs to be redrawn. |

## Property Information
| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Surface` | `SurfaceProperty` | `null` | `AffectsRender` |

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `ContentControl`
- `RenderSurface2DDrawContext`
- `RenderSurface2DDrawEventHandler`
- `MonoGameDrawingBackend`
