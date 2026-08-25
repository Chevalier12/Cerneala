using Cerneala.Drawing.Paths;
using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

internal static class MonoGamePathMeshBuilder
{
    public static MonoGamePathMesh Build(
        string pathData,
        DrawRect sourceBounds,
        float physicalWidth,
        float physicalHeight,
        float phaseX,
        float phaseY,
        XnaColor color)
    {
        return Build(
            DrawPathParser.ParseSvg(pathData),
            sourceBounds,
            physicalWidth,
            physicalHeight,
            phaseX,
            phaseY,
            color,
            DrawFillRule.NonZero);
    }

    public static MonoGamePathMesh Build(
        DrawPath path,
        DrawRect sourceBounds,
        float physicalWidth,
        float physicalHeight,
        float phaseX,
        float phaseY,
        XnaColor color,
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
        VertexPositionColor[] meshVertices = new VertexPositionColor[tessellator.Vertices.Length];
        for (int index = 0; index < tessellator.Vertices.Length; index++)
        {
            Vec3 position = tessellator.Vertices[index].Position;
            meshVertices[index] = new VertexPositionColor(new Vector3(position.X, position.Y, 0), color);
        }

        int[] indices = tessellator.Elements
            .Where(index => index != Tess.Undef)
            .ToArray();
        return new MonoGamePathMesh(meshVertices, indices);
    }
}

internal sealed record MonoGamePathMesh(VertexPositionColor[] Vertices, int[] Indices)
{
    public bool IsEmpty => Vertices.Length == 0 || Indices.Length < 3;
}
