namespace Cerneala.Drawing;

public readonly partial record struct DrawCommand
{
    public static DrawCommand DrawImage(
        IDrawImage image,
        DrawRect destination,
        DrawImageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _ = DrawImageGeometry.ResolveSource(image, options);
        return new DrawCommand(
            DrawCommandKind.DrawImage,
            destination,
            DrawImageGeometry.EffectiveTint(options),
            0,
            null,
            null,
            default,
            default,
            image,
            null,
            null,
            1,
            imageSource: options.Source,
            imageRotation: options.Rotation,
            imageOrigin: options.Origin,
            imageFlip: options.Flip,
            layerDepth: options.LayerDepth,
            imageOptions: options);
    }

    public static DrawCommand DrawImageQuad(
        IDrawImage image,
        DrawVertex2D topLeft,
        DrawVertex2D topRight,
        DrawVertex2D bottomRight,
        DrawVertex2D bottomLeft,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        DrawImageOptions options = new(
            layerDepth: layerDepth,
            sampling: sampling,
            addressMode: addressMode);
        DrawMesh2D mesh = new(
            [topLeft, topRight, bottomRight, bottomLeft],
            [0, 1, 2, 0, 2, 3],
            image: image);
        return CreateMeshCommand(
            DrawCommandKind.DrawImageQuad,
            mesh,
            image,
            options);
    }

    public static DrawCommand DrawImageQuad(
        IDrawImage image,
        DrawPoint topLeft,
        DrawPoint topRight,
        DrawPoint bottomRight,
        DrawPoint bottomLeft,
        DrawImageOptions? options = null)
    {
        options ??= new DrawImageOptions();
        DrawPoint[] textureCoordinates =
            DrawImageGeometry.GetTextureCoordinates(image, options);
        Color tint = DrawImageGeometry.EffectiveTint(options);
        return DrawImageQuad(
            image,
            new DrawVertex2D(topLeft, tint, textureCoordinates[0]),
            new DrawVertex2D(topRight, tint, textureCoordinates[1]),
            new DrawVertex2D(bottomRight, tint, textureCoordinates[2]),
            new DrawVertex2D(bottomLeft, tint, textureCoordinates[3]),
            options.Sampling,
            options.AddressMode,
            options.LayerDepth);
    }

    public static DrawCommand DrawNineSlice(
        IDrawImage image,
        DrawRect destination,
        DrawInsets insets,
        DrawImageOptions? options = null)
    {
        options ??= new DrawImageOptions();
        DrawMesh2D mesh = CreateNineSliceMesh(
            image,
            destination,
            insets,
            options);
        return new DrawCommand(
            DrawCommandKind.DrawNineSlice,
            mesh.Bounds,
            DrawImageGeometry.EffectiveTint(options),
            0,
            null,
            null,
            default,
            default,
            image,
            null,
            null,
            1,
            imageSource: options.Source,
            imageRotation: options.Rotation,
            imageOrigin: options.Origin,
            imageFlip: options.Flip,
            layerDepth: options.LayerDepth,
            imageOptions: options,
            insets: insets,
            mesh: mesh);
    }

    public static DrawCommand DrawMesh(
        DrawMesh2D mesh,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        DrawImageOptions options = new(
            layerDepth: layerDepth,
            sampling: sampling,
            addressMode: addressMode);
        return CreateMeshCommand(
            DrawCommandKind.DrawMesh,
            mesh,
            mesh.Image,
            options);
    }

    public static DrawCommand DrawTriangles(
        IEnumerable<DrawVertex2D> vertices,
        IDrawImage? image = null,
        DrawSamplingMode sampling = DrawSamplingMode.Linear,
        DrawAddressMode addressMode = DrawAddressMode.Clamp,
        float layerDepth = 0)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        DrawVertex2D[] copied = vertices.ToArray();
        DrawMesh2D mesh = new(
            copied,
            Enumerable.Range(0, copied.Length),
            DrawPrimitiveTopology.TriangleList,
            image);
        return DrawMesh(mesh, sampling, addressMode, layerDepth);
    }

    public static DrawCommand DrawPointBatch(DrawPointBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return new DrawCommand(
            DrawCommandKind.DrawPointBatch,
            batch.Bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            mesh: batch.Mesh,
            pointBatch: batch);
    }

    public static DrawCommand DrawLineBatch(DrawLineBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return new DrawCommand(
            DrawCommandKind.DrawLineBatch,
            batch.Bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            mesh: batch.Mesh,
            lineBatch: batch);
    }

    public static DrawCommand DrawSpriteBatch(DrawSpriteBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        DrawImageOptions options = new(
            sampling: batch.Sampling,
            addressMode: batch.AddressMode);
        return new DrawCommand(
            DrawCommandKind.DrawSpriteBatch,
            batch.Bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            batch.Image,
            null,
            null,
            1,
            imageOptions: options,
            mesh: batch.Mesh,
            spriteBatch: batch);
    }

    internal static DrawCommand WithMesh(
        DrawCommand source,
        DrawMesh2D mesh,
        DrawPointBatch? pointBatch = null,
        DrawLineBatch? lineBatch = null,
        DrawSpriteBatch? spriteBatch = null) =>
        new(
            source.Kind,
            mesh.Bounds,
            source.Color,
            source.Thickness,
            source.Text,
            source.TextRun,
            source.Position,
            source.EndPoint,
            source.Image,
            source.Font,
            source.Brush,
            source.BrushOpacity,
            source.PathData,
            source.SourceRect,
            source.PrismScope,
            source.RenderSurface,
            source.ImageSource,
            source.ImageRotation,
            source.ImageOrigin,
            source.ImageFlip,
            source.LayerDepth,
            source.Path,
            source.FillRule,
            source.Pen,
            source.Transform,
            source.Opacity,
            source.BlendMode,
            source.LayerOptions,
            source.CornerRadius,
            source.ImageOptions,
            source.Insets,
            mesh,
            pointBatch ?? source.PointBatch,
            lineBatch ?? source.LineBatch,
            spriteBatch ?? source.SpriteBatch);

    private static DrawCommand CreateMeshCommand(
        DrawCommandKind kind,
        DrawMesh2D mesh,
        IDrawImage? image,
        DrawImageOptions options) =>
        new(
            kind,
            mesh.Bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            image,
            null,
            null,
            1,
            layerDepth: options.LayerDepth,
            imageOptions: options,
            mesh: mesh);

    private static DrawMesh2D CreateNineSliceMesh(
        IDrawImage image,
        DrawRect destination,
        DrawInsets insets,
        DrawImageOptions options)
    {
        DrawRect source = DrawImageGeometry.ResolveSource(image, options);
        if (insets.Left + insets.Right > source.Width ||
            insets.Top + insets.Bottom > source.Height)
        {
            throw new ArgumentException(
                "Nine-slice insets must fit inside the selected source region.",
                nameof(insets));
        }

        (float destinationLeft, float destinationRight) = FitPair(
            insets.Left,
            insets.Right,
            destination.Width);
        (float destinationTop, float destinationBottom) = FitPair(
            insets.Top,
            insets.Bottom,
            destination.Height);
        float[] x =
        [
            0,
            destinationLeft,
            destination.Width - destinationRight,
            destination.Width
        ];
        float[] y =
        [
            0,
            destinationTop,
            destination.Height - destinationBottom,
            destination.Height
        ];
        float[] u =
        [
            source.X / image.Width,
            (source.X + insets.Left) / image.Width,
            (source.Right - insets.Right) / image.Width,
            source.Right / image.Width
        ];
        float[] v =
        [
            source.Y / image.Height,
            (source.Y + insets.Top) / image.Height,
            (source.Bottom - insets.Bottom) / image.Height,
            source.Bottom / image.Height
        ];
        if ((options.Flip & DrawImageFlip.Horizontal) != 0)
        {
            Array.Reverse(u);
        }
        if ((options.Flip & DrawImageFlip.Vertical) != 0)
        {
            Array.Reverse(v);
        }

        DrawVertex2D[] vertices = new DrawVertex2D[16];
        Color tint = DrawImageGeometry.EffectiveTint(options);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                vertices[(row * 4) + column] = new DrawVertex2D(
                    DrawImageGeometry.TransformDestinationPoint(
                        image,
                        destination,
                        options,
                        x[column],
                        y[row]),
                    tint,
                    new DrawPoint(u[column], v[row]));
            }
        }

        int[] indices = new int[54];
        int next = 0;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                int topLeft = (row * 4) + column;
                indices[next++] = topLeft;
                indices[next++] = topLeft + 1;
                indices[next++] = topLeft + 5;
                indices[next++] = topLeft;
                indices[next++] = topLeft + 5;
                indices[next++] = topLeft + 4;
            }
        }

        return new DrawMesh2D(vertices, indices, image: image);
    }

    private static (float First, float Second) FitPair(
        float first,
        float second,
        float available)
    {
        float total = first + second;
        if (total <= available || total <= float.Epsilon)
        {
            return (first, second);
        }

        float scale = available / total;
        return (first * scale, second * scale);
    }
}
