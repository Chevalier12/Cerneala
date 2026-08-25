using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

public sealed partial class RenderSurface2DFrame
{
    public void DrawImage(
        IDrawImage image,
        DrawRect destination,
        DrawImageOptions options)
    {
        EnsureActive();
        drawingContext.DrawImage(image, destination, options);
    }

    public void DrawImageQuad(
        IDrawImage image,
        DrawVertex2D topLeft,
        DrawVertex2D topRight,
        DrawVertex2D bottomRight,
        DrawVertex2D bottomLeft,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        EnsureActive();
        drawingContext.DrawImageQuad(
            image,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft,
            sampling,
            addressMode,
            layerDepth);
    }

    public void DrawImageQuad(
        IDrawImage image,
        DrawPoint topLeft,
        DrawPoint topRight,
        DrawPoint bottomRight,
        DrawPoint bottomLeft,
        DrawImageOptions? options = null)
    {
        EnsureActive();
        drawingContext.DrawImageQuad(
            image,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft,
            options);
    }

    public void DrawNineSlice(
        IDrawImage image,
        DrawRect destination,
        DrawInsets insets,
        DrawImageOptions? options = null)
    {
        EnsureActive();
        drawingContext.DrawNineSlice(image, destination, insets, options);
    }

    public void DrawMesh(
        DrawMesh2D mesh,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(mesh);
        drawingContext.DrawMesh(mesh, sampling, addressMode, layerDepth);
    }

    public void DrawTriangles(
        IEnumerable<DrawVertex2D> vertices,
        IDrawImage? image = null,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        EnsureActive();
        drawingContext.DrawTriangles(
            vertices,
            image,
            sampling,
            addressMode,
            layerDepth);
    }

    public void DrawPointBatch(DrawPointBatch batch)
    {
        EnsureActive();
        drawingContext.DrawPointBatch(batch);
    }

    public void DrawLineBatch(DrawLineBatch batch)
    {
        EnsureActive();
        drawingContext.DrawLineBatch(batch);
    }

    public void DrawSpriteBatch(DrawSpriteBatch batch)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(batch);
        drawingContext.DrawSpriteBatch(batch);
    }
}
