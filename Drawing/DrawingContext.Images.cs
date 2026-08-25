using Cerneala.Drawing.Prism;

namespace Cerneala.Drawing;

public sealed partial class DrawingContext
{
    public void DrawImage(
        IDrawImage image,
        DrawRect destination,
        DrawImageOptions options)
    {
        AddImageBackedCommand(
            image,
            resolved => DrawCommand.DrawImage(
                resolved,
                destination,
                options));
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
        AddImageBackedCommand(
            image,
            resolved => DrawCommand.DrawImageQuad(
                resolved,
                topLeft,
                topRight,
                bottomRight,
                bottomLeft,
                sampling,
                addressMode,
                layerDepth));
    }

    public void DrawImageQuad(
        IDrawImage image,
        DrawPoint topLeft,
        DrawPoint topRight,
        DrawPoint bottomRight,
        DrawPoint bottomLeft,
        DrawImageOptions? options = null)
    {
        AddImageBackedCommand(
            image,
            resolved => DrawCommand.DrawImageQuad(
                resolved,
                topLeft,
                topRight,
                bottomRight,
                bottomLeft,
                options));
    }

    public void DrawNineSlice(
        IDrawImage image,
        DrawRect destination,
        DrawInsets insets,
        DrawImageOptions? options = null)
    {
        AddImageBackedCommand(
            image,
            resolved => DrawCommand.DrawNineSlice(
                resolved,
                destination,
                insets,
                options));
    }

    public void DrawMesh(
        DrawMesh2D mesh,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.Image is null)
        {
            _commands.Add(DrawCommand.DrawMesh(
                mesh,
                sampling,
                addressMode,
                layerDepth));
            return;
        }

        AddImageBackedCommand(
            mesh.Image,
            resolved => DrawCommand.DrawMesh(
                ReferenceEquals(resolved, mesh.Image)
                    ? mesh
                    : mesh.WithImage(resolved),
                sampling,
                addressMode,
                layerDepth));
    }

    public void DrawTriangles(
        IEnumerable<DrawVertex2D> vertices,
        IDrawImage? image = null,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        if (image is null)
        {
            _commands.Add(DrawCommand.DrawTriangles(
                vertices,
                sampling: sampling,
                addressMode: addressMode,
                layerDepth: layerDepth));
            return;
        }

        DrawVertex2D[] copied = vertices?.ToArray() ??
            throw new ArgumentNullException(nameof(vertices));
        AddImageBackedCommand(
            image,
            resolved => DrawCommand.DrawTriangles(
                copied,
                resolved,
                sampling,
                addressMode,
                layerDepth));
    }

    public void DrawPointBatch(DrawPointBatch batch)
    {
        _commands.Add(DrawCommand.DrawPointBatch(batch));
    }

    public void DrawLineBatch(DrawLineBatch batch)
    {
        _commands.Add(DrawCommand.DrawLineBatch(batch));
    }

    public void DrawSpriteBatch(DrawSpriteBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        AddImageBackedCommand(
            batch.Image,
            resolved => DrawCommand.DrawSpriteBatch(
                ReferenceEquals(resolved, batch.Image)
                    ? batch
                    : batch.WithImage(resolved)));
    }

    private void AddImageBackedCommand(
        IDrawImage image,
        Func<IDrawImage, DrawCommand> createCommand)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(createCommand);
        if (image is PrismImage prismImage)
        {
            DrawCommand command = createCommand(prismImage.Source);
            _commands.Add(DrawCommand.BeginPrism(
                prismImage.CreateDrawScope(command.Rect)));
            _commands.Add(command);
            _commands.Add(DrawCommand.EndPrism());
            return;
        }

        _commands.Add(createCommand(image));
    }
}
