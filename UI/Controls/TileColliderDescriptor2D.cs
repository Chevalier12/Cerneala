using System.Numerics;
using static Cerneala.UI.Controls.Scene2DModelValidator;

namespace Cerneala.UI.Controls;

public enum TileColliderShape2D
{
    Box,
    Circle,
    Polygon,
    Segment
}

public sealed class TileColliderDescriptor2D
{
    public TileColliderDescriptor2D(
        TileColliderShape2D shape,
        float width = 1,
        float height = 1,
        float radius = 1,
        string points = "0,0 1,0 0,1",
        float offsetX = 0,
        float offsetY = 0,
        uint collisionLayer = 1,
        uint collisionMask = uint.MaxValue,
        bool isTrigger = false,
        string? debugIdentity = null,
        IReadOnlyDictionary<string, object?>? properties = null)
        : this(shape, Matrix3x2.Identity, width, height, radius, points, offsetX, offsetY,
            collisionLayer, collisionMask, isTrigger, debugIdentity, properties)
    {
    }

    public TileColliderDescriptor2D(
        TileColliderShape2D shape,
        Matrix3x2 localTransform,
        float width = 1,
        float height = 1,
        float radius = 1,
        string points = "0,0 1,0 0,1",
        float offsetX = 0,
        float offsetY = 0,
        uint collisionLayer = 1,
        uint collisionMask = uint.MaxValue,
        bool isTrigger = false,
        string? debugIdentity = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!float.IsFinite(localTransform.M11) || !float.IsFinite(localTransform.M12) ||
            !float.IsFinite(localTransform.M21) || !float.IsFinite(localTransform.M22) ||
            !float.IsFinite(localTransform.M31) || !float.IsFinite(localTransform.M32) ||
            !float.IsFinite(localTransform.GetDeterminant()) || !Matrix3x2.Invert(localTransform, out _))
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(localTransform), "Collider local transform must be finite and invertible."), "SCN2D008");
        }
        if (!Enum.IsDefined(shape))
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(shape)), "SCN2D008");
        }
        if (!float.IsFinite(offsetX) || !float.IsFinite(offsetY))
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(offsetX), "Collider offsets must be finite."), "SCN2D008");
        }
        if (shape == TileColliderShape2D.Box &&
            (!float.IsFinite(width) || width <= 0 || !float.IsFinite(height) || height <= 0))
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(width), "Box dimensions must be finite and positive."), "SCN2D008");
        }
        if (shape == TileColliderShape2D.Circle && (!float.IsFinite(radius) || radius <= 0))
        {
            throw Diagnostic(new ArgumentOutOfRangeException(nameof(radius), "Circle radius must be finite and positive."), "SCN2D008");
        }

        IReadOnlyList<Vector2> vertices = shape == TileColliderShape2D.Polygon
            ? PolygonCollider2D.ParsePoints(points)
            : shape == TileColliderShape2D.Segment ? ParseSegment(points)
            : Array.Empty<Vector2>();
        LocalTransform = localTransform;
        Shape = shape;
        Width = width;
        Height = height;
        Radius = radius;
        Points = points;
        Vertices = vertices;
        OffsetX = offsetX;
        OffsetY = offsetY;
        CollisionLayer = collisionLayer;
        CollisionMask = collisionMask;
        IsTrigger = isTrigger;
        DebugIdentity = debugIdentity;
        Properties = TileMapModelCopy.CopyProperties(properties);
        ValidateGeometry(Matrix3x2.Identity);
    }

    public TileColliderShape2D Shape { get; }

    public Matrix3x2 LocalTransform { get; }

    public float Width { get; }

    public float Height { get; }

    public float Radius { get; }

    public string Points { get; }

    public IReadOnlyList<Vector2> Vertices { get; }

    public float OffsetX { get; }

    public float OffsetY { get; }

    public uint CollisionLayer { get; }

    public uint CollisionMask { get; }

    public bool IsTrigger { get; }

    public string? DebugIdentity { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    internal ColliderLocalShape2D CreateLocalShape(float? boxWidth = null, float? boxHeight = null) => Shape switch
    {
        TileColliderShape2D.Box => ColliderLocalShape2D.Box(boxWidth ?? Width, boxHeight ?? Height),
        TileColliderShape2D.Circle => ColliderLocalShape2D.Circle(Radius),
        TileColliderShape2D.Polygon => ColliderLocalShape2D.Polygon(Vertices),
        TileColliderShape2D.Segment => ColliderLocalShape2D.Segment(Vertices),
        _ => throw new InvalidOperationException("The descriptor shape was not validated.")
    };

    internal void ValidateGeometry(Matrix3x2 placement)
    {
        try
        {
            Matrix3x2 transform = Matrix3x2.CreateTranslation(OffsetX, OffsetY) * LocalTransform * placement;
            if (!SceneGeometry2D.TryGetColliderBounds(CreateLocalShape(), transform, out _))
            {
                throw new ArgumentException("Collider geometry exceeds the finite scene coordinate range.");
            }
        }
        catch (ArgumentException error) { throw Diagnostic(error, "SCN2D008"); }
    }

    private static IReadOnlyList<Vector2> ParseSegment(string points)
    {
        IReadOnlyList<Vector2> vertices = Scene2DModelValidator.ParseShapePoints(points, 2, 2);
        SegmentCollider2D.ValidateEndpoints(vertices[0], vertices[1]);
        return vertices;
    }
}
