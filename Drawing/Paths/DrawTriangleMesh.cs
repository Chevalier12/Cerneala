namespace Cerneala.Drawing.Paths;

internal sealed record DrawTriangleMesh(DrawPoint[] Vertices, int[] Indices)
{
    public bool IsEmpty => Vertices.Length == 0 || Indices.Length < 3;
}

internal readonly record struct DrawStrokeRenderMesh(
    DrawTriangleMesh Mesh,
    DrawPoint[] BrushPoints,
    int Left,
    int Top);
