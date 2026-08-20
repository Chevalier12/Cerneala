# RenderSurface2DDrawEventHandler Delegate

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2D.cs`

Represents a callback that draws one managed `RenderSurface2D` frame.

```csharp
public delegate void RenderSurface2DDrawEventHandler(
    RenderSurface2DDrawContext context);
```

## Parameters
| Name | Type | Description |
| --- | --- | --- |
| `context` | `RenderSurface2DDrawContext` | Provides the raw MonoGame drawing resources and local surface bounds. |

## Examples
```csharp
surface.DrawSurface += context =>
{
    context.Begin();
    context.SpriteBatch.Draw(texture, context.Bounds, Color.White);
    context.End();
};
```

## Remarks
The callback executes inside the Cerneala-owned render loop. It can configure all `SpriteBatch.Begin` settings through the context and can perform multiple rendering passes. The callback must not present the graphics device or start a separate game loop.

## Applies To
Project: `Cerneala`

Backend: MonoGame/WindowsDX retained rendering.

## See Also
- `RenderSurface2D`
- `RenderSurface2DDrawContext`
