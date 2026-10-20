namespace Cerneala.Drawing.Paths;

// Edge-antialiased triangle mesh. A vertex is drawn at its position moved by its
// extrusion, a vector in physical pixels (about 0.5 px long across an edge), and
// paints its coverage (0..1) of the brush. Positions lie on the exact shape edges,
// so a consumer that ignores extrusion and coverage (a stencil clip) sees the
// exact shape: fringe triangles collapse to zero area.
internal sealed record DrawTriangleMesh(
    DrawPoint[] Vertices,
    DrawPoint[] Extrusions,
    float[] Coverages,
    int[] Indices)
{
    public static DrawTriangleMesh Empty { get; } = new([], [], [], []);

    public bool IsEmpty => Vertices.Length == 0 || Indices.Length < 3;
}

internal readonly record struct DrawStrokeRenderMesh(
    DrawTriangleMesh Mesh,
    DrawPoint[] BrushPoints,
    int Left,
    int Top);
