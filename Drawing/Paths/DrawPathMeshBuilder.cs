using LibTessDotNet;

namespace Cerneala.Drawing.Paths;

internal static class DrawPathMeshBuilder
{
    public static DrawTriangleMesh Build(
        DrawPath path,
        DrawRect sourceBounds,
        float physicalWidth,
        float physicalHeight,
        float phaseX,
        float phaseY,
        DrawFillRule fillRule)
    {
        ArgumentNullException.ThrowIfNull(path);
        float scaleX = physicalWidth / sourceBounds.Width;
        float scaleY = physicalHeight / sourceBounds.Height;
        float tolerance = 0.05f / MathF.Max(scaleX, scaleY);
        IReadOnlyList<DrawPoint[]> contours = DrawPathFlattener.Flatten(path, tolerance);
        Tess tessellator = new();
        foreach (DrawPoint[] contour in contours)
        {
            ContourVertex[] vertices = new ContourVertex[contour.Length];
            for (int index = 0; index < contour.Length; index++)
            {
                DrawPoint point = contour[index];
                vertices[index].Position = new Vec3(
                    phaseX + ((point.X - sourceBounds.X) * scaleX),
                    phaseY + ((point.Y - sourceBounds.Y) * scaleY),
                    0);
            }
            tessellator.AddContour(vertices, ContourOrientation.Original);
        }

        tessellator.Tessellate(
            fillRule == DrawFillRule.EvenOdd
                ? WindingRule.EvenOdd
                : WindingRule.NonZero,
            ElementType.Polygons,
            3);
        DrawPoint[] meshVertices = new DrawPoint[tessellator.Vertices.Length];
        for (int index = 0; index < tessellator.Vertices.Length; index++)
        {
            Vec3 position = tessellator.Vertices[index].Position;
            meshVertices[index] = new DrawPoint(position.X, position.Y);
        }

        int[] indices = tessellator.Elements
            .Where(index => index != Tess.Undef)
            .ToArray();
        return new DrawTriangleMesh(meshVertices, indices);
    }
}
