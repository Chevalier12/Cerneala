using System.Collections.ObjectModel;

namespace Cerneala.Drawing;

public readonly record struct DrawLineSegment2D
{
    public DrawLineSegment2D(
        DrawPoint start,
        DrawPoint end,
        Color color,
        float thickness = 1)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        Start = start;
        End = end;
        Color = color;
        Thickness = thickness;
    }

    public DrawPoint Start { get; }

    public DrawPoint End { get; }

    public Color Color { get; }

    public float Thickness { get; }
}

public sealed record DrawSprite2D
{
    public DrawSprite2D(
        DrawRect destination,
        DrawImageOptions? options = null)
    {
        Destination = destination;
        Options = options ?? new DrawImageOptions();
    }

    public DrawRect Destination { get; }

    public DrawImageOptions Options { get; }
}

public sealed class DrawPointBatch
{
    private readonly ReadOnlyCollection<DrawPoint> points;

    public DrawPointBatch(
        IEnumerable<DrawPoint> points,
        Color color,
        float diameter = 1)
    {
        ArgumentNullException.ThrowIfNull(points);
        DrawArgument.ThrowIfNotValidPixelSize(diameter, nameof(diameter));
        DrawPoint[] copied = points.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException(
                "A point batch cannot be empty.",
                nameof(points));
        }

        this.points = Array.AsReadOnly(copied);
        Color = color;
        Diameter = diameter;
        Mesh = DrawBatchMeshBuilder.BuildPoints(copied, color, diameter);
    }

    public IReadOnlyList<DrawPoint> Points => points;

    public Color Color { get; }

    public float Diameter { get; }

    public long Version => Mesh.Version;

    public DrawRect Bounds => Mesh.Bounds;

    internal DrawMesh2D Mesh { get; }

    internal DrawPointBatch Transform(
        Func<DrawPoint, DrawPoint> transform,
        float opacity = 1) =>
        new(this, Mesh.Transform(transform, opacity));

    private DrawPointBatch(DrawPointBatch source, DrawMesh2D mesh)
    {
        points = source.points;
        Color = source.Color;
        Diameter = source.Diameter;
        Mesh = mesh;
    }
}

public sealed class DrawLineBatch
{
    private readonly ReadOnlyCollection<DrawLineSegment2D> lines;

    public DrawLineBatch(IEnumerable<DrawLineSegment2D> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        DrawLineSegment2D[] copied = lines.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException(
                "A line batch cannot be empty.",
                nameof(lines));
        }

        this.lines = Array.AsReadOnly(copied);
        Mesh = DrawBatchMeshBuilder.BuildLines(copied);
    }

    public IReadOnlyList<DrawLineSegment2D> Lines => lines;

    public long Version => Mesh.Version;

    public DrawRect Bounds => Mesh.Bounds;

    internal DrawMesh2D Mesh { get; }

    internal DrawLineBatch Transform(
        Func<DrawPoint, DrawPoint> transform,
        float opacity = 1) =>
        new(this, Mesh.Transform(transform, opacity));

    private DrawLineBatch(DrawLineBatch source, DrawMesh2D mesh)
    {
        lines = source.lines;
        Mesh = mesh;
    }
}

public sealed class DrawSpriteBatch
{
    private readonly ReadOnlyCollection<DrawSprite2D> sprites;

    public DrawSpriteBatch(
        IDrawImage image,
        IEnumerable<DrawSprite2D> sprites)
    {
        DrawImageGeometry.ValidateImage(image);
        ArgumentNullException.ThrowIfNull(sprites);
        DrawSprite2D[] copied = sprites.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException(
                "A sprite batch cannot be empty.",
                nameof(sprites));
        }
        if (copied.Any(sprite =>
            sprite.Options.Sampling != copied[0].Options.Sampling ||
            sprite.Options.AddressMode != copied[0].Options.AddressMode))
        {
            throw new ArgumentException(
                "Every sprite in one batch must use the same sampling and address modes.",
                nameof(sprites));
        }

        Image = image;
        this.sprites = Array.AsReadOnly(copied);
        Sampling = copied[0].Options.Sampling;
        AddressMode = copied[0].Options.AddressMode;
        Mesh = DrawBatchMeshBuilder.BuildSprites(image, copied);
    }

    public IDrawImage Image { get; }

    public IReadOnlyList<DrawSprite2D> Sprites => sprites;

    public DrawSamplingMode Sampling { get; }

    public DrawAddressMode AddressMode { get; }

    public long Version => Mesh.Version;

    public DrawRect Bounds => Mesh.Bounds;

    internal DrawMesh2D Mesh { get; }

    internal DrawSpriteBatch Transform(
        Func<DrawPoint, DrawPoint> transform,
        float opacity = 1) =>
        new(this, Mesh.Transform(transform, opacity));

    internal DrawSpriteBatch WithImage(IDrawImage image) =>
        new(this, image, Mesh.WithImage(image));

    private DrawSpriteBatch(DrawSpriteBatch source, DrawMesh2D mesh)
    {
        Image = source.Image;
        sprites = source.sprites;
        Sampling = source.Sampling;
        AddressMode = source.AddressMode;
        Mesh = mesh;
    }

    private DrawSpriteBatch(
        DrawSpriteBatch source,
        IDrawImage image,
        DrawMesh2D mesh)
    {
        Image = image;
        sprites = source.sprites;
        Sampling = source.Sampling;
        AddressMode = source.AddressMode;
        Mesh = mesh;
    }
}

internal static class DrawBatchMeshBuilder
{
    private static readonly int[] QuadIndices = [0, 1, 2, 0, 2, 3];

    public static DrawMesh2D BuildPoints(
        IReadOnlyList<DrawPoint> points,
        Color color,
        float diameter)
    {
        DrawVertex2D[] vertices = new DrawVertex2D[points.Count * 4];
        int[] indices = new int[points.Count * 6];
        float half = diameter / 2;
        for (int index = 0; index < points.Count; index++)
        {
            DrawPoint point = points[index];
            int vertex = index * 4;
            vertices[vertex] = new DrawVertex2D(
                new DrawPoint(point.X - half, point.Y - half), color);
            vertices[vertex + 1] = new DrawVertex2D(
                new DrawPoint(point.X + half, point.Y - half), color);
            vertices[vertex + 2] = new DrawVertex2D(
                new DrawPoint(point.X + half, point.Y + half), color);
            vertices[vertex + 3] = new DrawVertex2D(
                new DrawPoint(point.X - half, point.Y + half), color);
            CopyQuadIndices(indices, index * 6, vertex);
        }

        return new DrawMesh2D(vertices, indices);
    }

    public static DrawMesh2D BuildLines(
        IReadOnlyList<DrawLineSegment2D> lines)
    {
        DrawVertex2D[] vertices = new DrawVertex2D[lines.Count * 4];
        int[] indices = new int[lines.Count * 6];
        for (int index = 0; index < lines.Count; index++)
        {
            DrawLineSegment2D line = lines[index];
            float deltaX = line.End.X - line.Start.X;
            float deltaY = line.End.Y - line.Start.Y;
            float length = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            float half = line.Thickness / 2;
            float normalX = length <= float.Epsilon ? half : (-deltaY / length) * half;
            float normalY = length <= float.Epsilon ? 0 : (deltaX / length) * half;
            int vertex = index * 4;
            vertices[vertex] = new DrawVertex2D(
                new DrawPoint(line.Start.X + normalX, line.Start.Y + normalY),
                line.Color);
            vertices[vertex + 1] = new DrawVertex2D(
                new DrawPoint(line.End.X + normalX, line.End.Y + normalY),
                line.Color);
            vertices[vertex + 2] = new DrawVertex2D(
                new DrawPoint(line.End.X - normalX, line.End.Y - normalY),
                line.Color);
            vertices[vertex + 3] = new DrawVertex2D(
                new DrawPoint(line.Start.X - normalX, line.Start.Y - normalY),
                line.Color);
            CopyQuadIndices(indices, index * 6, vertex);
        }

        return new DrawMesh2D(vertices, indices);
    }

    public static DrawMesh2D BuildSprites(
        IDrawImage image,
        IReadOnlyList<DrawSprite2D> sprites)
    {
        DrawVertex2D[] vertices = new DrawVertex2D[sprites.Count * 4];
        int[] indices = new int[sprites.Count * 6];
        for (int index = 0; index < sprites.Count; index++)
        {
            DrawSprite2D sprite = sprites[index];
            DrawPoint[] positions = DrawImageGeometry.GetDestinationCorners(
                image,
                sprite.Destination,
                sprite.Options);
            DrawPoint[] textureCoordinates = DrawImageGeometry.GetTextureCoordinates(
                image,
                sprite.Options);
            Color tint = DrawImageGeometry.EffectiveTint(sprite.Options);
            int vertex = index * 4;
            for (int corner = 0; corner < 4; corner++)
            {
                vertices[vertex + corner] = new DrawVertex2D(
                    positions[corner],
                    tint,
                    textureCoordinates[corner]);
            }
            CopyQuadIndices(indices, index * 6, vertex);
        }

        return new DrawMesh2D(vertices, indices, image: image);
    }

    private static void CopyQuadIndices(
        int[] target,
        int targetOffset,
        int vertexOffset)
    {
        for (int index = 0; index < QuadIndices.Length; index++)
        {
            target[targetOffset + index] = vertexOffset + QuadIndices[index];
        }
    }
}
