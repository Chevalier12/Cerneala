using System.Collections.ObjectModel;

namespace Cerneala.Drawing;

public enum DrawPrimitiveTopology
{
    TriangleList,
    TriangleStrip
}

public readonly record struct DrawVertex2D
{
    public DrawVertex2D(
        DrawPoint position,
        Color color,
        DrawPoint textureCoordinate = default)
    {
        Position = position;
        Color = color;
        TextureCoordinate = textureCoordinate;
    }

    public DrawPoint Position { get; }

    public Color Color { get; }

    public DrawPoint TextureCoordinate { get; }
}

public sealed class DrawMesh2D
{
    private static long nextVersion;
    private readonly ReadOnlyCollection<DrawVertex2D> vertices;
    private readonly ReadOnlyCollection<int> indices;

    public DrawMesh2D(
        IEnumerable<DrawVertex2D> vertices,
        IEnumerable<int> indices,
        DrawPrimitiveTopology topology = DrawPrimitiveTopology.TriangleList,
        IDrawImage? image = null)
        : this(
            Copy(vertices, nameof(vertices)),
            Copy(indices, nameof(indices)),
            topology,
            image,
            Interlocked.Increment(ref nextVersion))
    {
    }

    private DrawMesh2D(
        DrawVertex2D[] vertices,
        int[] indices,
        DrawPrimitiveTopology topology,
        IDrawImage? image,
        long version)
    {
        Validate(vertices, indices, topology, image);
        VertexArray = vertices;
        IndexArray = indices;
        this.vertices = Array.AsReadOnly(vertices);
        this.indices = Array.AsReadOnly(indices);
        Topology = topology;
        Image = image;
        Version = version;
        Bounds = CalculateBounds(vertices);
    }

    public IReadOnlyList<DrawVertex2D> Vertices => vertices;

    public IReadOnlyList<int> Indices => indices;

    public DrawPrimitiveTopology Topology { get; }

    public IDrawImage? Image { get; }

    public long Version { get; }

    public DrawRect Bounds { get; }

    internal DrawVertex2D[] VertexArray { get; }

    internal int[] IndexArray { get; }

    internal DrawMesh2D Transform(
        Func<DrawPoint, DrawPoint> transform,
        float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(transform);
        DrawVertex2D[] transformed = new DrawVertex2D[VertexArray.Length];
        for (int index = 0; index < transformed.Length; index++)
        {
            DrawVertex2D vertex = VertexArray[index];
            transformed[index] = new DrawVertex2D(
                transform(vertex.Position),
                DrawImageGeometry.ApplyOpacity(vertex.Color, opacity),
                vertex.TextureCoordinate);
        }

        return new DrawMesh2D(
            transformed,
            IndexArray,
            Topology,
            Image,
            Version);
    }

    internal DrawMesh2D WithImage(IDrawImage image)
    {
        DrawImageGeometry.ValidateImage(image);
        return new DrawMesh2D(
            VertexArray,
            IndexArray,
            Topology,
            image,
            Version);
    }

    private static void Validate(
        DrawVertex2D[] vertices,
        int[] indices,
        DrawPrimitiveTopology topology,
        IDrawImage? image)
    {
        if (!Enum.IsDefined(topology))
        {
            throw new ArgumentOutOfRangeException(nameof(topology));
        }
        if (vertices.Length < 3)
        {
            throw new ArgumentException(
                "A 2D mesh requires at least three vertices.",
                nameof(vertices));
        }
        if (indices.Length < 3 ||
            (topology == DrawPrimitiveTopology.TriangleList &&
                indices.Length % 3 != 0))
        {
            throw new ArgumentException(
                topology == DrawPrimitiveTopology.TriangleList
                    ? "Triangle-list indices must contain complete triangles."
                    : "A triangle strip requires at least three indices.",
                nameof(indices));
        }
        foreach (int index in indices)
        {
            if ((uint)index >= (uint)vertices.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(indices),
                    "Every mesh index must reference an existing vertex.");
            }
        }
        if (image is not null)
        {
            DrawImageGeometry.ValidateImage(image);
        }
    }

    private static DrawRect CalculateBounds(DrawVertex2D[] vertices)
    {
        float left = vertices[0].Position.X;
        float top = vertices[0].Position.Y;
        float right = left;
        float bottom = top;
        for (int index = 1; index < vertices.Length; index++)
        {
            DrawPoint point = vertices[index].Position;
            left = MathF.Min(left, point.X);
            top = MathF.Min(top, point.Y);
            right = MathF.Max(right, point.X);
            bottom = MathF.Max(bottom, point.Y);
        }

        return new DrawRect(left, top, right - left, bottom - top);
    }

    private static T[] Copy<T>(IEnumerable<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        return values.ToArray();
    }
}
