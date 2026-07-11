# DrawBrushStretch Enum

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Controls how tile content is fitted into its destination.

```csharp
public enum DrawBrushStretch
```

## Examples
```csharp
var brush = new ImageBrush(image, stretch: DrawBrushStretch.Uniform);
```

## Members
| Name | Description |
| --- | --- |
| `None` | Keeps source size. |
| `Fill` | Fills the destination, allowing aspect-ratio distortion. |
| `Uniform` | Fits while preserving aspect ratio. |
| `UniformToFill` | Covers the destination while preserving aspect ratio. |

## Applies to
`TileBrush` derivatives.
