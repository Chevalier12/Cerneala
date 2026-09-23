# RenderSurface3DDrawEventHandler Delegate

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/RenderSurface3D.cs`

Represents a callback that records one `RenderSurface3D` frame.

```csharp
public delegate void RenderSurface3DDrawEventHandler(RenderSurface3D sender, RenderSurface3DFrame frame);
```

## Examples
```csharp
RenderSurface3DDrawEventHandler draw = (_, frame) =>
    frame.DrawMarker(Vector3.Zero, Color.White);
surface.Draw += draw;
```

## Remarks
The frame is valid only during callback dispatch. An exception aborts the recording and is propagated. Invalidations or camera mutations made by a callback remain pending for a later frame.

## Applies to
`Cerneala`
