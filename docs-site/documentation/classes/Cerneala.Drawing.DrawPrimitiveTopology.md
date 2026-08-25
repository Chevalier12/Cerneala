# DrawPrimitiveTopology Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawMesh2D.cs`

Specifies how indexed 2D mesh vertices form triangles.

```csharp
public enum DrawPrimitiveTopology
```

## Examples

```csharp
DrawMesh2D mesh = new(vertices, indices, DrawPrimitiveTopology.TriangleStrip);
drawing.DrawMesh(mesh);
```

## Remarks

Triangle-list indices must contain complete groups of three. A triangle strip requires at least three indices.

## Values

| Name | Description |
| --- | --- |
| `TriangleList` | Every consecutive group of three indices forms one triangle. |
| `TriangleStrip` | Each index after the first two completes another connected triangle. |

## Applies To

`DrawMesh2D` and mesh drawing commands.
