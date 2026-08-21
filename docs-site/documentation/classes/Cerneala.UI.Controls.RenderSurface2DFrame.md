# RenderSurface2DFrame Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `Drawing/MonoGame/RenderSurface2DFrame.cs`

Provides specialized 2D drawing operations for one `RenderSurface2D` frame.

```csharp
public sealed class RenderSurface2DFrame
```

## Examples
Draw a colored platform and a sprite during the surface callback.

```csharp
surface.Draw += (_, frame) =>
{
    frame.FillRectangle(new DrawRect(0, 180, 320, 20), Color.DarkSlateGray);
    frame.DrawSprite(player, new DrawRect(48, 148, 32, 32), Color.White);
};
```

Draw a region from a sprite sheet with rotation and horizontal flipping.

```csharp
frame.DrawSprite(
    atlas,
    destination: new DrawRect(96, 64, 32, 32),
    source: new DrawRect(128, 0, 32, 32),
    tint: Color.White,
    rotation: MathF.PI / 4,
    origin: new DrawPoint(16, 16),
    flip: RenderSurface2DSpriteFlip.Horizontal,
    layerDepth: 0.5f);
```

## Remarks
`RenderSurface2DFrame` is created and owned by Cerneala. It is valid only while `RenderSurface2D.OnDraw` and the surface's `Draw` subscribers execute. Calling a drawing method after the callback returns throws `ObjectDisposedException`.

`Bounds` uses local render-target pixels and begins at `(0, 0)`. Sprite rotation is expressed in radians, and `origin` uses source-image pixels.

The frame deliberately does not expose a graphics device, render target, or begin/end operations. Cerneala supplies fixed 2D render states and owns presentation.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `DrawRect` | Gets the local pixel bounds of the surface. |
| `FrameTime` | `TimeSpan` | Gets the elapsed frame time supplied by the Cerneala render loop. |

## Methods
| Name | Return type | Description |
| --- | --- | --- |
| `FillRectangle(DrawRect, Color)` | `void` | Fills a pixel-space rectangle with a color. |
| `DrawSprite(IDrawImage, DrawRect, Color)` | `void` | Draws an entire image into a destination rectangle. |
| `DrawSprite(IDrawImage, DrawRect, DrawRect?, Color, float, DrawPoint, RenderSurface2DSpriteFlip, float)` | `void` | Draws an optional source region with tint, rotation, origin, flip, and layer depth. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| Drawing methods | `ObjectDisposedException` | The frame callback has already completed. |
| `DrawSprite` | `ArgumentNullException` | `image` is `null`. |
| `DrawSprite` | `ArgumentOutOfRangeException` | Rotation is not finite, layer depth is outside `0` through `1`, or flip contains unsupported flags. |
| `DrawSprite` | `InvalidOperationException` | The image is incompatible with the active drawing backend or graphics device. |

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `RenderSurface2D`
- `RenderSurface2DDrawEventHandler`
- `RenderSurface2DSpriteFlip`
- `IDrawImage`
