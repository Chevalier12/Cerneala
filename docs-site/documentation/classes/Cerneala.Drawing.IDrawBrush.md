# IDrawBrush Interface

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Backend-neutral contract implemented by semantic UI brushes.

```csharp
public interface IDrawBrush
```

## Examples
```csharp
IDrawBrush brush = new SolidColorBrush(Color.CornflowerBlue);
DrawCommand command = DrawCommand.FillRectangle(new DrawRect(0, 0, 80, 24), brush);
```

## Remarks
The interface is marked `EditorBrowsable(Never)` because application code should normally use `Cerneala.UI.Media.Brush`. Drawing backends use `CreateDescriptor()` to obtain an immutable device-neutral description.

## Properties
| Name | Description |
| --- | --- |
| `Kind` | Semantic brush kind. |
| `Opacity` | Brush opacity in `0..1`. |
| `SolidColor` | Solid color shortcut, or `null` for composite brushes. |

## Methods
| Name | Description |
| --- | --- |
| `CreateDescriptor()` | Creates the representation consumed by a drawing backend. |

## Applies to
`Cerneala` on `net8.0-windows`.

## See also
- `Cerneala.UI.Media.Brush`
- `Cerneala.Drawing.DrawBrushDescriptor`
