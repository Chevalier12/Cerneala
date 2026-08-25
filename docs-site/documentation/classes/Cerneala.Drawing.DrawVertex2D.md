# DrawVertex2D Struct

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawMesh2D.cs`

Represents one platform-neutral colored and optionally textured 2D vertex.

```csharp
public readonly record struct DrawVertex2D
```

## Examples

```csharp
DrawVertex2D vertex = new(
    new DrawPoint(20, 12),
    Color.White,
    new DrawPoint(1, 0));
```

## Remarks

Texture coordinates are normalized coordinates interpreted when the containing mesh has an image. Vertex color modulates that image; it is the rendered color for an untextured mesh.

## Constructors

| Name | Description |
| --- | --- |
| `DrawVertex2D(DrawPoint position, Color color, DrawPoint textureCoordinate = default)` | Creates a vertex with position, color, and optional texture coordinate. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Position` | `DrawPoint` | Gets the local 2D position. |
| `Color` | `Color` | Gets the vertex color or texture modulation color. |
| `TextureCoordinate` | `DrawPoint` | Gets the normalized texture coordinate. |

## Applies To

`DrawMesh2D`, `DrawTriangles`, and explicit image quads.
