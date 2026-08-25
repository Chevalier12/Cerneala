using Cerneala.Drawing.Paths;
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
        DrawTriangleMesh mesh = DrawPathMeshBuilder.Build(
            path,
            sourceBounds,
            physicalWidth,
            physicalHeight,
            phaseX,
            phaseY,
            fillRule);
        VertexPositionColor[] meshVertices = new VertexPositionColor[mesh.Vertices.Length];
        for (int index = 0; index < mesh.Vertices.Length; index++)
        {
            DrawPoint position = mesh.Vertices[index];
            meshVertices[index] = new VertexPositionColor(
                new Vector3(position.X, position.Y, 0),
                color);
        }
        return new MonoGamePathMesh(meshVertices, mesh.Indices);
    }
}

internal sealed record MonoGamePathMesh(VertexPositionColor[] Vertices, int[] Indices)
{
    public bool IsEmpty => Vertices.Length == 0 || Indices.Length < 3;
}
