using System.Numerics;
using System.Runtime.InteropServices;

namespace Cerneala.Backends.SdlGpu;

// Only Point+Clamp image commands carry these five geometric edge functions.
// Ordinary drawing retains its 32-byte vertex and existing shader pipeline.
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct SdlGpuImageDomainVertex(
    Vector2 Position,
    Vector2 TextureCoordinate,
    Vector4 Color,
    Vector3 FirstEdges,
    Vector2 LastEdges);

internal readonly record struct SdlGpuImageDomainEdges(
    float E01,
    float E12,
    float E02,
    float E23,
    float E30);

// The one E02 field is shared by both triangle membership tests. Independent
// barycentric reconstructions would create rounding holes along their common
// diagonal. Each edge is scaled by its maximum magnitude at the four corners,
// so thin but finite triangles do not require unrepresentable inverse areas.
internal readonly struct SdlGpuImageDomainGeometry
{
    private readonly NormalizedEdge e01;
    private readonly NormalizedEdge e12;
    private readonly NormalizedEdge e02;
    private readonly NormalizedEdge e23;
    private readonly NormalizedEdge e30;
    private readonly bool firstTriangleCollapsed;
    private readonly bool secondTriangleCollapsed;

    private SdlGpuImageDomainGeometry(
        Vector2 q0,
        Vector2 q1,
        Vector2 q2,
        Vector2 q3,
        bool firstTriangleCollapsed,
        bool secondTriangleCollapsed)
    {
        e01 = new NormalizedEdge(q0, q1, q0, q1, q2, q3);
        e12 = new NormalizedEdge(q1, q2, q0, q1, q2, q3);
        e02 = new NormalizedEdge(q0, q2, q0, q1, q2, q3);
        e23 = new NormalizedEdge(q2, q3, q0, q1, q2, q3);
        e30 = new NormalizedEdge(q3, q0, q0, q1, q2, q3);
        this.firstTriangleCollapsed = firstTriangleCollapsed;
        this.secondTriangleCollapsed = secondTriangleCollapsed;
    }

    public static bool TryCreate(
        Vector2 q0,
        Vector2 q1,
        Vector2 q2,
        Vector2 q3,
        out SdlGpuImageDomainGeometry geometry)
    {
        bool firstCollapsed = Cross(q0, q1, q2) == 0;
        bool secondCollapsed = Cross(q0, q2, q3) == 0;
        if (firstCollapsed && secondCollapsed)
        {
            geometry = default;
            return false;
        }

        geometry = new SdlGpuImageDomainGeometry(
            q0, q1, q2, q3, firstCollapsed, secondCollapsed);
        return true;
    }

    public SdlGpuImageDomainEdges Map(Vector2 position) => new(
        firstTriangleCollapsed ? 1 : e01.At(position),
        firstTriangleCollapsed ? -1 : e12.At(position),
        e02.At(position),
        secondTriangleCollapsed ? 1 : e23.At(position),
        secondTriangleCollapsed ? -1 : e30.At(position));

    private static double Cross(Vector2 origin, Vector2 end, Vector2 point)
    {
        double ax = (double)end.X - origin.X;
        double ay = (double)end.Y - origin.Y;
        double bx = (double)point.X - origin.X;
        double by = (double)point.Y - origin.Y;
        return (ax * by) - (ay * bx);
    }

    private readonly struct NormalizedEdge
    {
        private readonly Vector2 origin;
        private readonly Vector2 end;
        private readonly double maximum;

        public NormalizedEdge(
            Vector2 origin,
            Vector2 end,
            Vector2 q0,
            Vector2 q1,
            Vector2 q2,
            Vector2 q3)
        {
            this.origin = origin;
            this.end = end;
            maximum = Math.Max(
                Math.Max(Math.Abs(Cross(origin, end, q0)),
                    Math.Abs(Cross(origin, end, q1))),
                Math.Max(Math.Abs(Cross(origin, end, q2)),
                    Math.Abs(Cross(origin, end, q3))));
        }

        public float At(Vector2 position) => maximum == 0
            ? 0
            : (float)(Cross(origin, end, position) / maximum);
    }
}
