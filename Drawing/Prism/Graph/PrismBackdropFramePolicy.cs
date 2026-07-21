using System.Numerics;

namespace Cerneala.Drawing.Prism.Graph;

internal readonly record struct PrismBackdropFrameDescriptor(
    BackdropFrameMetadata Metadata,
    long SourceIdentity)
{
    public PrismGraphDependency Dependency =>
        CreateDependency(lowerUiVersion: 0);

    public PrismGraphDependency CreateDependency(long lowerUiVersion)
    {
        if (lowerUiVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lowerUiVersion));
        }

        long version = lowerUiVersion == 0
            ? Metadata.ContentVersion
            : ComposeContentVersion(
                Metadata.ContentVersion,
                lowerUiVersion);
        return
        new(
            PrismGraphDependencyKind.BackdropFrame,
            SourceIdentity,
            version);
    }

    private static long ComposeContentVersion(
        long contentVersion,
        long lowerUiVersion)
    {
        const ulong offset = 14695981039346656037;
        ulong hash = offset;
        Mix(ref hash, (uint)contentVersion);
        Mix(ref hash, (uint)((ulong)contentVersion >> 32));
        Mix(ref hash, (uint)lowerUiVersion);
        Mix(ref hash, (uint)((ulong)lowerUiVersion >> 32));
        long version = (long)(hash & long.MaxValue);
        return version == 0 ? 1 : version;
    }

    private static void Mix(ref ulong hash, uint value)
    {
        const ulong prime = 1099511628211;
        hash = unchecked((hash ^ value) * prime);
    }
}

internal static class PrismBackdropFramePolicy
{
    public static PrismBackdropFrameDescriptor Prepare(
        in BackdropFrameMetadata metadata)
    {
        return Prepare(in metadata, default);
    }

    public static PrismBackdropFrameDescriptor Prepare(
        in BackdropFrameMetadata metadata,
        PrismBackdropSourceToken sourceToken)
    {
        Validate(metadata);
        return new PrismBackdropFrameDescriptor(
            metadata,
            sourceToken.Value);
    }

    public static DrawRect CalculateSourceBounds(
        in PrismBackdropFrameDescriptor frame,
        DrawRect hostBounds)
    {
        Matrix3x2 transform = frame.Metadata.CoordinateTransform;
        Vector2 topLeft = Vector2.Transform(
            new Vector2(hostBounds.X, hostBounds.Y),
            transform);
        Vector2 topRight = Vector2.Transform(
            new Vector2(hostBounds.Right, hostBounds.Y),
            transform);
        Vector2 bottomLeft = Vector2.Transform(
            new Vector2(hostBounds.X, hostBounds.Bottom),
            transform);
        Vector2 bottomRight = Vector2.Transform(
            new Vector2(hostBounds.Right, hostBounds.Bottom),
            transform);

        float left = Math.Clamp(
            MathF.Floor(
                MathF.Min(
                    MathF.Min(topLeft.X, topRight.X),
                    MathF.Min(bottomLeft.X, bottomRight.X))),
            0,
            frame.Metadata.PixelWidth);
        float top = Math.Clamp(
            MathF.Floor(
                MathF.Min(
                    MathF.Min(topLeft.Y, topRight.Y),
                    MathF.Min(bottomLeft.Y, bottomRight.Y))),
            0,
            frame.Metadata.PixelHeight);
        float right = Math.Clamp(
            MathF.Ceiling(
                MathF.Max(
                    MathF.Max(topLeft.X, topRight.X),
                    MathF.Max(bottomLeft.X, bottomRight.X))),
            0,
            frame.Metadata.PixelWidth);
        float bottom = Math.Clamp(
            MathF.Ceiling(
                MathF.Max(
                    MathF.Max(topLeft.Y, topRight.Y),
                    MathF.Max(bottomLeft.Y, bottomRight.Y))),
            0,
            frame.Metadata.PixelHeight);

        return new DrawRect(
            left,
            top,
            MathF.Max(0, right - left),
            MathF.Max(0, bottom - top));
    }

    private static void Validate(BackdropFrameMetadata metadata)
    {
        if (metadata.PixelWidth <= 0 ||
            metadata.PixelHeight <= 0 ||
            !float.IsFinite(metadata.PixelScale) ||
            metadata.PixelScale <= 0 ||
            metadata.ContentVersion < 0)
        {
            throw new ArgumentException(
                "Backdrop frame metadata is incomplete or invalid.",
                nameof(metadata));
        }
        if (!Enum.IsDefined(metadata.ColorProfile) ||
            !Enum.IsDefined(metadata.PixelFormat) ||
            !Enum.IsDefined(metadata.AlphaMode))
        {
            throw new ArgumentException(
                "Backdrop frame metadata contains an unsupported color profile, pixel format, or alpha mode.",
                nameof(metadata));
        }

        Matrix3x2 transform = metadata.CoordinateTransform;
        if (!IsFinite(transform) ||
            !float.IsFinite(transform.GetDeterminant()) ||
            MathF.Abs(transform.GetDeterminant()) <= 0.000001f)
        {
            throw new ArgumentException(
                "Backdrop frame metadata requires a finite invertible coordinate transform.",
                nameof(metadata));
        }
    }

    private static bool IsFinite(Matrix3x2 matrix) =>
        float.IsFinite(matrix.M11) &&
        float.IsFinite(matrix.M12) &&
        float.IsFinite(matrix.M21) &&
        float.IsFinite(matrix.M22) &&
        float.IsFinite(matrix.M31) &&
        float.IsFinite(matrix.M32);
}
