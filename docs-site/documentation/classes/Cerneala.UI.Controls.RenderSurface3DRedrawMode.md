# RenderSurface3DRedrawMode Enumeration

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/RenderSurface3DRedrawMode.cs`

Selects automatic continuous recording or explicit on-demand invalidation.

```csharp
public enum RenderSurface3DRedrawMode
```

## Examples
```csharp
surface.RedrawMode = RenderSurface3DRedrawMode.OnDemand;
surface.InvalidateFrame();
```

## Remarks
`OnDemand` is the `RenderSurface3D` default. `Continuous` requests a new recording on each framework frame while a draw subscriber is present.

| Value | Description |
| --- | --- |
| `Continuous` (`0`) | Request recording each framework frame while at least one `Draw` handler is subscribed. |
| `OnDemand` (`1`) | Request recording only after relevant invalidation. This is the control default. |

## Applies to
`Cerneala`
