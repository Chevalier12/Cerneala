# RenderSurface2DDrawContext Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2DDrawContext.cs`

Provides unrestricted MonoGame drawing access for one managed `RenderSurface2D` callback.

```csharp
public sealed class RenderSurface2DDrawContext
```

## Examples
Configure a custom effect and point sampling, draw one pass, and close the batch explicitly.

```csharp
context.Begin(
    sortMode: SpriteSortMode.Immediate,
    blendState: BlendState.AlphaBlend,
    samplerState: SamplerState.PointClamp,
    depthStencilState: DepthStencilState.None,
    rasterizerState: RasterizerState.CullNone,
    effect: pixelEffect,
    transformMatrix: cameraMatrix);
context.SpriteBatch.Draw(atlas, context.Bounds, Color.White);
context.End();
```

## Remarks
The context exposes the raw `SpriteBatch` and its `GraphicsDevice`. Cerneala owns the outer frame loop and render target but does not restrict the batch configuration used inside the surface.

`Begin` mirrors all `SpriteBatch.Begin` arguments and supplies conventional 2D defaults for omitted states. A callback can call `Begin` and `End` multiple times for multipass rendering. If a callback returns while a batch started through `Begin` remains active, Cerneala ends that batch before invoking another callback or returning to retained UI rendering.

`Bounds` uses local render-target pixels and begins at `(0, 0)`. Direct graphics-device drawing is allowed; the MonoGame backend restores its render target, viewport, clip, and render states after the callback.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `SpriteBatch` | `SpriteBatch` | Gets the raw MonoGame batch allocated for this managed surface. |
| `GraphicsDevice` | `GraphicsDevice` | Gets the graphics device used by the surface and host. |
| `Bounds` | `Rectangle` | Gets the local pixel bounds of the offscreen surface. |
| `IsBatchActive` | `bool` | Indicates whether a batch started through `Begin` is currently active. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `Begin(...)` | `void` | Starts the surface batch with caller-selected MonoGame settings. |
| `End()` | `void` | Ends the active batch started through `Begin`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Begin` | `InvalidOperationException` | A context-managed batch is already active. |
| `End` | `InvalidOperationException` | No context-managed batch is active. |

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `RenderSurface2D`
- `RenderSurface2DDrawEventHandler`
