# TileDrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Common backend descriptor for image, drawing, and visual tile brushes.

```csharp
public abstract record TileDrawBrushDescriptor(
    DrawBrushStretch Stretch,
    DrawBrushAlignmentX AlignmentX,
    DrawBrushAlignmentY AlignmentY,
    DrawRect? Viewport,
    DrawRect? Viewbox,
    DrawTileMode TileMode,
    float BrushOpacity) : DrawBrushDescriptor(BrushOpacity)
```

## Examples
```csharp
TileDrawBrushDescriptor descriptor = ((IDrawBrush)imageBrush).CreateDescriptor() as TileDrawBrushDescriptor
    ?? throw new InvalidOperationException();
```

## Properties
| Name | Description |
| --- | --- |
| `Stretch` | Fitting policy. |
| `AlignmentX` | Horizontal alignment. |
| `AlignmentY` | Vertical alignment. |
| `Viewport` | Optional destination tile rectangle. |
| `Viewbox` | Optional source rectangle. |
| `TileMode` | Repetition policy. |
| `BrushOpacity` | Source opacity. |

## Applies to
Backend implementation code.
