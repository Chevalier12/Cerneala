using System.Numerics;

namespace Cerneala.Drawing;

public enum RenderProjection3DKind
{
    Perspective,
    Orthographic
}

public readonly record struct RenderProjection3D
{
    private RenderProjection3D(RenderProjection3DKind kind, float verticalFieldOfViewOrHeight, float nearPlane, float farPlane)
    {
        Kind = kind;
        VerticalFieldOfViewOrHeight = verticalFieldOfViewOrHeight;
        NearPlane = nearPlane;
        FarPlane = farPlane;
    }

    public RenderProjection3DKind Kind { get; }
    public float VerticalFieldOfViewOrHeight { get; }
    public float NearPlane { get; }
    public float FarPlane { get; }

    public static RenderProjection3D Perspective(float verticalFieldOfView, float nearPlane, float farPlane)
    {
        ValidatePlanes(nearPlane, farPlane);
        if (!float.IsFinite(verticalFieldOfView) || verticalFieldOfView <= 0 || verticalFieldOfView >= MathF.PI)
        {
            throw new ArgumentOutOfRangeException(nameof(verticalFieldOfView));
        }
        return new(RenderProjection3DKind.Perspective, verticalFieldOfView, nearPlane, farPlane);
    }

    public static RenderProjection3D Orthographic(float height, float nearPlane, float farPlane)
    {
        ValidatePlanes(nearPlane, farPlane);
        if (!float.IsFinite(height) || height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
        return new(RenderProjection3DKind.Orthographic, height, nearPlane, farPlane);
    }

    internal Matrix4x4 CreateMatrix(float aspectRatio)
    {
        Validate(this);
        if (!float.IsFinite(aspectRatio) || aspectRatio <= 0) { throw new ArgumentOutOfRangeException(nameof(aspectRatio)); }
        return Kind == RenderProjection3DKind.Perspective
            ? Matrix4x4.CreatePerspectiveFieldOfView(VerticalFieldOfViewOrHeight, aspectRatio, NearPlane, FarPlane)
            : Matrix4x4.CreateOrthographic(VerticalFieldOfViewOrHeight * aspectRatio, VerticalFieldOfViewOrHeight, NearPlane, FarPlane);
    }

    internal static bool IsValid(RenderProjection3D value)
    {
        try { Validate(value); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    internal static void Validate(RenderProjection3D value)
    {
        if (!Enum.IsDefined(value.Kind)) { throw new ArgumentOutOfRangeException(nameof(value)); }
        ValidatePlanes(value.NearPlane, value.FarPlane);
        if (!float.IsFinite(value.VerticalFieldOfViewOrHeight) || value.VerticalFieldOfViewOrHeight <= 0 ||
            (value.Kind == RenderProjection3DKind.Perspective && value.VerticalFieldOfViewOrHeight >= MathF.PI))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    private static void ValidatePlanes(float nearPlane, float farPlane)
    {
        if (!float.IsFinite(nearPlane) || !float.IsFinite(farPlane) || nearPlane <= 0 || farPlane <= nearPlane)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlane));
        }
    }
}

public readonly record struct DrawRay3D(Vector3 Origin, Vector3 Direction);

public readonly record struct DrawLineSegment3D
{
    public DrawLineSegment3D(Vector3 start, Vector3 end, Color color, float thickness = 1)
    {
        RenderSurface3DValidation.ValidateVector(start, nameof(start));
        RenderSurface3DValidation.ValidateVector(end, nameof(end));
        RenderSurface3DValidation.ValidateColor(color, nameof(color));
        RenderSurface3DValidation.ValidateSize(thickness, nameof(thickness));
        Start = start; End = end; Color = color; Thickness = thickness;
    }
    public Vector3 Start { get; }
    public Vector3 End { get; }
    public Color Color { get; }
    public float Thickness { get; }
}

public readonly record struct DrawMarker3D
{
    public DrawMarker3D(Vector3 center, Color color, float diameter = 1)
    {
        RenderSurface3DValidation.ValidateVector(center, nameof(center));
        RenderSurface3DValidation.ValidateColor(color, nameof(color));
        RenderSurface3DValidation.ValidateSize(diameter, nameof(diameter));
        Center = center; Color = color; Diameter = diameter;
    }
    public Vector3 Center { get; }
    public Color Color { get; }
    public float Diameter { get; }
}

internal static class RenderSurface3DValidation
{
    internal static void ValidateVector(Vector3 value, string name)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z)) throw new ArgumentOutOfRangeException(name);
    }
    internal static void ValidateColor(Color value, string name)
    {
        if (value.A != byte.MaxValue) throw new ArgumentOutOfRangeException(name, "3D primitive colors must be opaque.");
    }
    internal static void ValidateSize(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }
    internal static bool IsFiniteAffine(Matrix4x4 value) =>
        float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M13) && float.IsFinite(value.M14) &&
        float.IsFinite(value.M21) && float.IsFinite(value.M22) && float.IsFinite(value.M23) && float.IsFinite(value.M24) &&
        float.IsFinite(value.M31) && float.IsFinite(value.M32) && float.IsFinite(value.M33) && float.IsFinite(value.M34) &&
        float.IsFinite(value.M41) && float.IsFinite(value.M42) && float.IsFinite(value.M43) && float.IsFinite(value.M44) &&
        value.M14 == 0 && value.M24 == 0 && value.M34 == 0 && value.M44 == 1;
}
