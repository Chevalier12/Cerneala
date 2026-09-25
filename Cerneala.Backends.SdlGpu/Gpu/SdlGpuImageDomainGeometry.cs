using System.Numerics;
using System.Runtime.InteropServices;

namespace Cerneala.Backends.SdlGpu;

// Only Point+Clamp image commands carry the complete physical image domain.
// Ordinary drawing retains its 32-byte vertex and existing shader pipeline.
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct SdlGpuImageDomainVertex(
    Vector2 Position,
    Vector2 TextureCoordinate,
    Vector4 Color,
    Vector4 FirstCorners,
    Vector4 LastCorners);

// The corners are the same target-local, post-transform physical positions
// used by the raster vertices. Every vertex of one logical image carries the
// same four corners, including every patch of a nine-slice image.
internal readonly struct SdlGpuImageDomainGeometry
{
    public Vector4 FirstCorners { get; }

    public Vector4 LastCorners { get; }

    private SdlGpuImageDomainGeometry(Vector2 q0, Vector2 q1, Vector2 q2, Vector2 q3)
    {
        FirstCorners = new Vector4(q0.X, q0.Y, q1.X, q1.Y);
        LastCorners = new Vector4(q2.X, q2.Y, q3.X, q3.Y);
    }

    public static bool TryCreate(
        Vector2 q0,
        Vector2 q1,
        Vector2 q2,
        Vector2 q3,
        out SdlGpuImageDomainGeometry geometry)
    {
        if (Cross(q0, q1, q2) == 0 && Cross(q0, q2, q3) == 0)
        {
            geometry = default;
            return false;
        }

        geometry = new SdlGpuImageDomainGeometry(q0, q1, q2, q3);
        return true;
    }

    private static double Cross(Vector2 origin, Vector2 end, Vector2 point)
    {
        double ax = (double)end.X - origin.X;
        double ay = (double)end.Y - origin.Y;
        double bx = (double)point.X - origin.X;
        double by = (double)point.Y - origin.Y;
        return (ax * by) - (ay * bx);
    }
}
