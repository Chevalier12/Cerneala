using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Xunit;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuImageDomainGeometryTests
{
    [Theory]
    [InlineData(0f, 0f, 8f, 0f, 8f, 8f, 0f, 8f)]
    [InlineData(8f, 0f, 0f, 0f, 0f, 8f, 8f, 8f)]
    [InlineData(1f, 2f, 7f, 3f, 9f, 10f, 3f, 9f)]
    public void LogicalQuadCarriesItsFourPhysicalCorners(
        float x0, float y0, float x1, float y1,
        float x2, float y2, float x3, float y3)
    {
        Vector2 q0 = new(x0, y0);
        Vector2 q1 = new(x1, y1);
        Vector2 q2 = new(x2, y2);
        Vector2 q3 = new(x3, y3);
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(q0, q1, q2, q3, out var domain));
        AssertCorners(domain, q0, q1, q2, q3);

        // One q0-q2 edge is used by both triangles. The shader receives these
        // identical flat corners on every vertex, not independent barycentrics.
        Assert.Equal(0, Cross(q0, q2, q0));
        Assert.Equal(0, Cross(q0, q2, q2));
        Assert.Equal(0, Cross(q0, q2, (q0 + q2) * 0.5f));
    }

    [Fact]
    public void LogicalCoordinatesSurviveReflectionRotationAndSkew()
    {
        Vector2[] rectangle =
        [
            new(2, 3), new(10, 3), new(10, 9), new(2, 9)
        ];
        Matrix3x2 transform = Matrix3x2.CreateScale(-1.25f, 0.75f) *
            Matrix3x2.CreateSkew(0.13f, -0.08f) *
            Matrix3x2.CreateRotation(0.2f) *
            Matrix3x2.CreateTranslation(34, 21);
        Vector2[] transformed = rectangle.Select(point =>
            Vector2.Transform(point, transform)).ToArray();
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            transformed[0], transformed[1], transformed[2], transformed[3], out var domain));
        AssertCorners(domain, transformed[0], transformed[1], transformed[2], transformed[3]);
        Assert.True(PreservesOrdinaryCenter(domain,
            Vector2.Transform(new Vector2(4, 5), transform)));
        Assert.False(PreservesOrdinaryCenter(domain,
            Vector2.Transform(new Vector2(25, 25), transform)));
    }

    [Theory]
    [InlineData(0f, 0f, 8f, 0f, 3f, 2f, 0f, 8f)] // concave
    [InlineData(0f, 0f, 8f, 8f, 0f, 8f, 8f, 0f)] // self-crossing
    [InlineData(0f, 0f, 4f, 0f, 8f, 0f, 0f, 8f)] // first collapsed
    [InlineData(0f, 0f, 8f, 0f, 8f, 8f, 4f, 4f)] // second collapsed
    [InlineData(0f, 0f, 0.0001f, 0f, 0.0001f, 0.00001f, 0f, 0.00001f)]
    public void ExistingFiniteTrianglesPreserveTheirPhysicalCorners(
        float x0, float y0, float x1, float y1,
        float x2, float y2, float x3, float y3)
    {
        Vector2 q0 = new(x0, y0);
        Vector2 q1 = new(x1, y1);
        Vector2 q2 = new(x2, y2);
        Vector2 q3 = new(x3, y3);
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(q0, q1, q2, q3, out var domain));
        AssertCorners(domain, q0, q1, q2, q3);
        Assert.True(AllFinite(domain));
    }

    [Fact]
    public void WhollyCollapsedQuadHasNoImageDomainOrRasterizedTriangle()
    {
        Assert.False(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(2, 0),
            new Vector2(4, 0), new Vector2(6, 0), out _));
    }

    [Fact]
    public void OneCollapsedConstituentDoesNotClassifyOutsideCenterAsInside()
    {
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(4, 0),
            new Vector2(8, 0), new Vector2(0, 8), out var domain));

        Assert.True(PreservesOrdinaryCenter(domain, new Vector2(1, 1)));
        Assert.False(PreservesOrdinaryCenter(domain, new Vector2(7, 2)));
    }

    [Fact]
    public void FlatQuadPayloadDoesNotIncreaseOrdinaryVertexLayout()
    {
        Assert.Equal(32, Marshal.SizeOf<SdlGpuVertex>());
        Assert.Equal(64, Marshal.SizeOf<SdlGpuImageDomainVertex>());
    }

    [Fact]
    public void ValidFiniteNearDegenerateQuadHasRepresentablePrivateDomain()
    {
        Vector2[] q = [new(0, 0), new(0, 1), new(1e-40f, 0), new(1, 1)];
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            q[0], q[1], q[2], q[3], out var domain));
        AssertCorners(domain, q[0], q[1], q[2], q[3]);
        Assert.True(AllFinite(domain));
    }

    [Fact]
    public void LargestFiniteQuadCoordinatesKeepPrivateDomainRepresentable()
    {
        float extent = float.MaxValue;
        Vector2[] q =
        [
            new(-extent, -extent), new(extent, -extent),
            new(extent, extent), new(-extent, extent)
        ];
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            q[0], q[1], q[2], q[3], out var domain));
        AssertCorners(domain, q[0], q[1], q[2], q[3]);
        Assert.True(AllFinite(domain));

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            q[3], q[2], q[1], q[0], out var reversed));
        AssertCorners(reversed, q[3], q[2], q[1], q[0]);
        Assert.True(AllFinite(reversed));
    }

    [Fact]
    public void ZeroLengthOuterEdgeDoesNotAdmitAnOutsideCenter()
    {
        Vector2 repeated = new(0, 0);
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            repeated, repeated, new Vector2(8, 8), new Vector2(0, 8), out var domain));

        Assert.True(PreservesOrdinaryCenter(domain, new Vector2(1, 6)));
        Assert.False(PreservesOrdinaryCenter(domain, new Vector2(7, 2)));
        AssertCorners(domain, repeated, repeated, new Vector2(8, 8), new Vector2(0, 8));
    }

    [Fact]
    public void AnalyticDomainDistinguishesExternalBoundaryFromInternalDiagonal()
    {
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(8, 0),
            new Vector2(8, 8), new Vector2(0, 8), out var rectangle));
        Assert.True(PreservesOrdinaryCenter(rectangle, new Vector2(4, 4)));
        Assert.False(PreservesOrdinaryCenter(rectangle, new Vector2(0, 4)));
        Assert.False(PreservesOrdinaryCenter(rectangle, new Vector2(8, 4)));
        Assert.False(PreservesOrdinaryCenter(rectangle, new Vector2(4, 0)));
        Assert.False(PreservesOrdinaryCenter(rectangle, new Vector2(4, 8)));
        Assert.False(PreservesOrdinaryCenter(rectangle, new Vector2(0, 0)));
        Assert.True(PreservesOrdinaryCenter(rectangle, new Vector2(4, 3)));
        Assert.True(PreservesOrdinaryCenter(rectangle,
            new Vector2(MathF.BitDecrement(8), 4)));
        Assert.False(PreservesOrdinaryCenter(rectangle,
            new Vector2(MathF.BitIncrement(8), 4)));

        // Both triangles lie above q0-q2: their shared edge is external.
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(4, 8),
            new Vector2(8, 0), new Vector2(8, 8), out var overlapping));
        Assert.False(PreservesOrdinaryCenter(overlapping, new Vector2(4, 0)));
        // One triangle's outer edge lies inside the other triangle.
        Assert.True(PreservesOrdinaryCenter(overlapping, new Vector2(6, 4)));

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(8, 8),
            new Vector2(8, 0), new Vector2(0, 8), out var crossing));
        Assert.False(PreservesOrdinaryCenter(crossing, new Vector2(4, 0)));
    }

    [Fact]
    public void CollapsedConstituentMakesFormerSharedEdgeExternal()
    {
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(4, 0),
            new Vector2(8, 0), new Vector2(0, 8), out var firstCollapsed));
        Assert.False(PreservesOrdinaryCenter(firstCollapsed, new Vector2(4, 0)));
        Assert.True(PreservesOrdinaryCenter(firstCollapsed, new Vector2(2, 2)));

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(8, 0),
            new Vector2(8, 8), new Vector2(4, 4), out var secondCollapsed));
        Assert.False(PreservesOrdinaryCenter(secondCollapsed, new Vector2(4, 4)));
        Assert.True(PreservesOrdinaryCenter(secondCollapsed, new Vector2(6, 2)));
    }

    private static void AssertCorners(
        SdlGpuImageDomainGeometry domain, Vector2 q0, Vector2 q1, Vector2 q2, Vector2 q3)
    {
        Assert.Equal(new Vector4(q0.X, q0.Y, q1.X, q1.Y), domain.FirstCorners);
        Assert.Equal(new Vector4(q2.X, q2.Y, q3.X, q3.Y), domain.LastCorners);
    }

    private static bool AllFinite(SdlGpuImageDomainGeometry domain)
    {
        Vector4 a = domain.FirstCorners;
        Vector4 b = domain.LastCorners;
        return float.IsFinite(a.X) && float.IsFinite(a.Y) &&
            float.IsFinite(a.Z) && float.IsFinite(a.W) &&
            float.IsFinite(b.X) && float.IsFinite(b.Y) &&
            float.IsFinite(b.Z) && float.IsFinite(b.W);
    }

    // Independent double-precision reference for the logical union. The
    // compiled shader is separately checked by native pixel-readback tests.
    private static bool PreservesOrdinaryCenter(SdlGpuImageDomainGeometry domain, Vector2 center)
    {
        Vector4 a = domain.FirstCorners;
        Vector4 b = domain.LastCorners;
        Vector2 q0 = new(a.X, a.Y);
        Vector2 q1 = new(a.Z, a.W);
        Vector2 q2 = new(b.X, b.Y);
        Vector2 q3 = new(b.Z, b.W);
        double e01 = Cross(q0, q1, center);
        double e12 = Cross(q1, q2, center);
        double e02 = Cross(q0, q2, center);
        double e23 = Cross(q2, q3, center);
        double e30 = Cross(q3, q0, center);
        bool firstAlive = Cross(q0, q1, q2) != 0;
        bool secondAlive = Cross(q0, q2, q3) != 0;
        bool firstPositive = e01 > 0 && e12 > 0;
        bool firstNegative = e01 < 0 && e12 < 0;
        bool secondPositive = e23 > 0 && e30 > 0;
        bool secondNegative = e23 < 0 && e30 < 0;
        bool sharedInternal = firstAlive && secondAlive && e02 == 0 &&
            ((firstPositive && secondPositive) || (firstNegative && secondNegative));
        return (firstAlive && e02 < 0 && firstPositive) ||
            (firstAlive && e02 > 0 && firstNegative) ||
            (secondAlive && e02 > 0 && secondPositive) ||
            (secondAlive && e02 < 0 && secondNegative) ||
            sharedInternal;
    }

    private static double Cross(Vector2 origin, Vector2 end, Vector2 point)
    {
        double ax = (double)end.X - origin.X;
        double ay = (double)end.Y - origin.Y;
        double bx = (double)point.X - origin.X;
        double by = (double)point.Y - origin.Y;
        return ax * by - ay * bx;
    }
}
