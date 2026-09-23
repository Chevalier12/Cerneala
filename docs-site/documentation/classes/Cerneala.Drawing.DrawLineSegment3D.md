# DrawLineSegment3D Struct

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/RenderProjection3D.cs`

Describes one opaque 3D line for `RenderSurface3DFrame.DrawLineBatch`.

```csharp
public readonly record struct DrawLineSegment3D
```

## Examples
```csharp
DrawLineSegment3D[] lines =
[
    new(Vector3.Zero, Vector3.UnitY, Color.White, thickness: 2)
];
surface.Draw += (_, frame) => frame.DrawLineBatch(lines);
```

## Remarks
The constructor requires finite endpoints, alpha 255, and finite positive `Thickness`; invalid arguments throw `ArgumentOutOfRangeException`. Thickness is a local DIP size. Lines have butt caps, are clipped to the near, far, and side planes, and use depth testing. A degenerate line covers no pixels. The frame copies batch values during the active callback; caller-owned storage is not retained. The struct's all-zero default bypasses constructor validation and is rejected by a non-empty batch.

## Properties
| Name | Description |
| --- | --- |
| `Start`, `End` | Line endpoints in local 3D coordinates before the optional model matrix. |
| `Color` | Opaque line color. |
| `Thickness` | Positive line width in local DIPs. |

## Constructors
| Signature | Description |
| --- | --- |
| `DrawLineSegment3D(Vector3 start, Vector3 end, Color color, float thickness = 1)` | Creates a validated opaque line. |

## Applies to
`Cerneala`
