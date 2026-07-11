# DrawBrushKind Enum

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Identifies the semantic content represented by an `IDrawBrush`.

```csharp
public enum DrawBrushKind
```

## Examples
```csharp
if (brush.Kind == DrawBrushKind.LinearGradient)
{
    // Select gradient-specific diagnostics or tooling.
}
```

## Remarks
The value is descriptive; rendering support is selected by the backend descriptor rather than by a switch in application code.

## Members
| Name | Description |
| --- | --- |
| `SolidColor` | A single color. |
| `LinearGradient` | A linear gradient. |
| `RadialGradient` | An elliptical radial gradient. |
| `Image` | Image content. |
| `Drawing` | Immutable draw-command content. |
| `Visual` | Captured live visual content. |

## Applies to
`Cerneala` on `net8.0-windows`.
