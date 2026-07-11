# DrawBrushDescriptor Class

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Base record for immutable brush descriptions passed to a drawing backend.

```csharp
public abstract record DrawBrushDescriptor(float Opacity)
```

## Examples
```csharp
DrawBrushDescriptor descriptor = ((IDrawBrush)brush).CreateDescriptor();
float opacity = descriptor.Opacity;
```

## Remarks
This is a backend-facing API and is marked `EditorBrowsable(Never)`. Use semantic brush classes for authoring. Concrete descriptor records carry gradient, image, drawing, or visual data.

## Properties
| Name | Description |
| --- | --- |
| `Opacity` | Effective opacity declared by the source brush. |

## Applies to
`Cerneala` drawing backends.

## See also
- `Cerneala.Drawing.IDrawBrush`
- `Cerneala.Drawing.TileDrawBrushDescriptor`
