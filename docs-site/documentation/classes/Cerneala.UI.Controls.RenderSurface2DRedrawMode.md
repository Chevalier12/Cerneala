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
| `OnDemand` | `1` | Redraws only after the surface becomes dirty. |

## Remarks
Use `Continuous` for animated gameplay. Use `OnDemand` for static scenes or previews, and call `RenderSurface2D.InvalidateFrame()` whenever their game content changes.

## Applies To
Project: `Cerneala`

## See Also
- `RenderSurface2D`
- `RenderSurface2D.InvalidateFrame()`
