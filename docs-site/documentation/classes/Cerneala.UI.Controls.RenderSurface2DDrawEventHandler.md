# RenderSurface2DDrawEventHandler Delegate

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2D.cs`

Represents a callback that draws one managed `RenderSurface2D` frame.

```csharp
public delegate void RenderSurface2DDrawEventHandler(
    RenderSurface2D sender,
    RenderSurface2DFrame frame);
```

## Parameters
| Name | Type | Description |
| --- | --- | --- |
| `sender` | `RenderSurface2D` | The surface that is rendering the frame. |
| `frame` | `RenderSurface2DFrame` | Provides the current frame timing, bounds, and specialized 2D drawing operations. |

## Examples
```csharp
surface.Draw += (_, frame) =>
{
    frame.FillRectangle(frame.Bounds, new Color(8, 11, 17));
    frame.DrawSprite(player, new DrawRect(48, 80, 32, 32), Color.White);
};
```

## Remarks
The callback executes inside the Cerneala-owned render loop after the surface has been cleared. The `frame` argument is valid only for the duration of the callback and does not expose the graphics device, render target, or batch lifecycle.

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `RenderSurface2D`
- `RenderSurface2DFrame`
