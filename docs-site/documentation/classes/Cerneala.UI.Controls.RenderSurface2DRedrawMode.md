# RenderSurface2DRedrawMode Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/RenderSurface2DRedrawMode.cs`

Specifies when a `RenderSurface2D` produces a new game frame.

```csharp
public enum RenderSurface2DRedrawMode
```

## Fields
| Name | Value | Description |
| --- | --- | --- |
| `Continuous` | `0` | Redraws during every Cerneala frame. |
| `OnDemand` | `1` | Redraws after the surface becomes dirty, including when a drawn `PrismImage` changes. |

## Remarks
Use `Continuous` when arbitrary frame state changes continuously. Use `OnDemand` for static or dependency-driven scenes. A `PrismImage` drawn in the current frame invalidates an on-demand surface automatically when its operation or pipeline state changes. Call `RenderSurface2D.InvalidateFrame()` for other changes that affect manually generated drawing commands.

## Applies To
Project: `Cerneala`

## See Also
- `RenderSurface2D`
- `RenderSurface2D.InvalidateFrame()`
