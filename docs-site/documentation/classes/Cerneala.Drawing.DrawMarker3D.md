# DrawMarker3D Struct

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/RenderProjection3D.cs`

Describes one opaque camera-facing disc for `RenderSurface3DFrame.DrawMarkerBatch`.

```csharp
public readonly record struct DrawMarker3D
```

## Examples
```csharp
DrawMarker3D[] markers = [new(Vector3.Zero, Color.CornflowerBlue, diameter: 12)];
surface.Draw += (_, frame) => frame.DrawMarkerBatch(markers);
```

## Remarks
The constructor requires a finite center, alpha 255, and a finite positive `Diameter`; invalid arguments throw `ArgumentOutOfRangeException`. Diameter is a local DIP size. A marker is a camera-facing circular disc with center depth and constant projected size, not a sphere. The frame copies non-empty batches while active; an all-zero default struct is rejected by a non-empty batch. A transparent clear color is allowed, but transparent marker geometry is not.

## Properties
| Name | Description |
| --- | --- |
| `Center` | Disc center in local 3D coordinates before the optional model matrix. |
| `Color` | Opaque disc color. |
| `Diameter` | Positive disc diameter in local DIPs. |

## Constructors
| Signature | Description |
| --- | --- |
| `DrawMarker3D(Vector3 center, Color color, float diameter = 1)` | Creates a validated opaque marker. |

## Applies to
`Cerneala`
