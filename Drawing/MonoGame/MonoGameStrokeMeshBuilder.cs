using Cerneala.Drawing.Paths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Drawing.MonoGame;

internal readonly record struct MonoGameStrokeMesh(
    MonoGamePathMesh Mesh,
    int Left,
    int Top);

internal static class MonoGameStrokeMeshBuilder
{
    public static MonoGameStrokeMesh Build(
        DrawCommand command,
        float coordinateScale,
        Func<DrawPoint, XnaColor> colorSelector)
    {
        DrawPen pen = command.Pen ??
            throw new ArgumentException(
                "A stroke command requires a pen.",
                nameof(command));
        return Build(
            command,
            pen.Thickness,
            pen.Style,
            coordinateScale,
            colorSelector);
    }

    public static MonoGameStrokeMesh Build(
        DrawCommand command,
        float thickness,
        DrawStrokeStyle style,
        float coordinateScale,
        Func<DrawPoint, XnaColor> colorSelector)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(colorSelector);

        DrawStrokeRenderMesh stroke = DrawStrokeMeshBuilder.Build(
            command,
            thickness,
            style,
            coordinateScale);
        VertexPositionColor[] vertices = new VertexPositionColor[stroke.Mesh.Vertices.Length];
        for (int index = 0; index < stroke.Mesh.Vertices.Length; index++)
        {
            DrawPoint point = stroke.Mesh.Vertices[index];
            vertices[index] = new VertexPositionColor(
                new Vector3(point.X, point.Y, 0),
                colorSelector(stroke.BrushPoints[index]));
        }

        return new MonoGameStrokeMesh(
            new MonoGamePathMesh(vertices, stroke.Mesh.Indices),
            stroke.Left,
            stroke.Top);
    }
}
