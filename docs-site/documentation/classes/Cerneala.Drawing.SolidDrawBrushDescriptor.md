# SolidDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Immutable backend descriptor for a solid brush.

```csharp
public sealed record SolidDrawBrushDescriptor(Color Color, float BrushOpacity)
    : DrawBrushDescriptor(BrushOpacity)
```

## Examples
```csharp
SolidDrawBrushDescriptor descriptor = new(Color.White, 0.75f);
```

## Remarks
The solid path is the renderer fast path and does not require a brush texture.

## Properties
| Name | Description |
| --- | --- |
| `Color` | Color to draw. |
| `BrushOpacity` | Source brush opacity; exposed as inherited `Opacity` too. |

## Applies to
Backend implementation code.
