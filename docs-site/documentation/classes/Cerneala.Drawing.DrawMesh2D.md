# DrawMesh2D Class

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawMesh2D.cs`

Represents an immutable, reusable, indexed 2D triangle mesh with optional image data.

```csharp
public sealed class DrawMesh2D
```

## Examples

```csharp
DrawMesh2D mesh = new(
    [
        new DrawVertex2D(new DrawPoint(0, 0), Color.Red),
        new DrawVertex2D(new DrawPoint(80, 0), Color.Green),
        new DrawVertex2D(new DrawPoint(40, 60), Color.Blue)
    ],
    [0, 1, 2]);

drawing.DrawMesh(mesh);
```

## Remarks

The constructor copies both input sequences and exposes read-only views, so later changes to caller-owned collections cannot change retained identity. `Version` is stable for the immutable mesh instance, and `Bounds` encloses all vertex positions.

A mesh may reference at most one platform-neutral `IDrawImage`. The mesh does not own or dispose that image; the caller must keep it valid through rendering. The backend does not create a per-mesh GPU resource, so texture lifetime remains owned by the image and backend device resources are recreated through the normal device lifecycle.

## Constructors

| Name | Description |
| --- | --- |
| `DrawMesh2D(IEnumerable<DrawVertex2D> vertices, IEnumerable<int> indices, DrawPrimitiveTopology topology = TriangleList, IDrawImage? image = null)` | Copies and validates an indexed colored or textured mesh. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Vertices` | `IReadOnlyList<DrawVertex2D>` | Gets the copied vertices. |
| `Indices` | `IReadOnlyList<int>` | Gets the copied indices. |
| `Topology` | `DrawPrimitiveTopology` | Gets the triangle topology. |
| `Image` | `IDrawImage?` | Gets the optional image used by all textured vertices. |
| `Version` | `long` | Gets the stable version assigned to this immutable payload. |
| `Bounds` | `DrawRect` | Gets the axis-aligned vertex bounds. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | A vertex or index sequence is null. |
| `ArgumentException` | Fewer than three vertices/indices are supplied, triangle-list indices are incomplete, or the image dimensions are invalid. |
| `ArgumentOutOfRangeException` | Topology is unsupported or an index does not reference an existing vertex. |

## Applies To

Reusable colored and textured geometry recorded by `DrawingContext` or `RenderSurface2DFrame`.

## See Also

- `DrawVertex2D`
- `DrawPrimitiveTopology`
- `DrawingContext.DrawMesh`
