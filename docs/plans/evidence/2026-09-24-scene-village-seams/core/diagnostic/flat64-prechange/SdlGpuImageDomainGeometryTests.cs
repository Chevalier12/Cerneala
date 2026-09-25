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
    public void SharedDiagonalUsesOneInterpolatedEdgeFunction(
        float x0, float y0, float x1, float y1,
        float x2, float y2, float x3, float y3)
    {
        Vector2 q0 = new(x0, y0);
        Vector2 q1 = new(x1, y1);
        Vector2 q2 = new(x2, y2);
        Vector2 q3 = new(x3, y3);
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(q0, q1, q2, q3, out var frame));

        SdlGpuImageDomainEdges first = frame.Map(q0);
        SdlGpuImageDomainEdges opposite = frame.Map(q2);
        SdlGpuImageDomainEdges middle = frame.Map((q0 + q2) * 0.5f);
        Assert.Equal(0f, first.E02);
        Assert.Equal(0f, opposite.E02);
        Assert.Equal(0f, middle.E02);
        Assert.True(AllFinite(frame.Map(q1)));
        Assert.True(AllFinite(frame.Map(q3)));
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
            transformed[0], transformed[1], transformed[2], transformed[3], out var frame));

        foreach (Vector2 corner in transformed)
        {
            Assert.True(CenterInside(frame.Map(corner)));
        }
        Vector2 interior = Vector2.Transform(new Vector2(4, 5), transform);
        Assert.True(CenterInside(frame.Map(interior)));
        Assert.False(CenterInside(frame.Map(
            Vector2.Transform(new Vector2(25, 25), transform))));
    }

    [Theory]
    [InlineData(0f, 0f, 8f, 0f, 3f, 2f, 0f, 8f)] // concave
    [InlineData(0f, 0f, 8f, 8f, 0f, 8f, 8f, 0f)] // self-crossing
    [InlineData(0f, 0f, 4f, 0f, 8f, 0f, 0f, 8f)] // first triangle collapsed
    [InlineData(0f, 0f, 8f, 0f, 8f, 8f, 4f, 4f)] // second triangle collapsed
    [InlineData(0f, 0f, 0.0001f, 0f, 0.0001f, 0.00001f, 0f, 0.00001f)]
    public void ExistingFiniteTrianglesProduceFiniteDomainPayload(
        float x0, float y0, float x1, float y1,
        float x2, float y2, float x3, float y3)
    {
        Vector2[] points =
        [
            new(x0, y0), new(x1, y1), new(x2, y2), new(x3, y3)
        ];
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            points[0], points[1], points[2], points[3], out var frame));

        foreach (Vector2 point in points)
        {
            Assert.True(AllFinite(frame.Map(point)));
        }
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
            new Vector2(8, 0), new Vector2(0, 8), out var frame));

        Assert.True(CenterInside(frame.Map(new Vector2(1, 1))));
        Assert.False(CenterInside(frame.Map(new Vector2(7, 2))));
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
        Vector2[] points =
        [
            new(0, 0), new(0, 1), new(1e-40f, 0), new(1, 1)
        ];

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            points[0], points[1], points[2], points[3], out var frame));
        foreach (Vector2 point in points)
        {
            Assert.True(AllFinite(frame.Map(point)));
        }
    }

    [Fact]
    public void LargestFiniteQuadCoordinatesKeepEveryPrivateEdgeRepresentable()
    {
        float extent = float.MaxValue;
        Vector2[] points =
        [
            new(-extent, -extent), new(extent, -extent),
            new(extent, extent), new(-extent, extent)
        ];

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            points[0], points[1], points[2], points[3], out var frame));
        foreach (Vector2 point in points)
        {
            Assert.True(AllFinite(frame.Map(point)));
        }

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            points[3], points[2], points[1], points[0], out var reversed));
        foreach (Vector2 point in points)
        {
            Assert.True(AllFinite(reversed.Map(point)));
        }
    }

    [Fact]
    public void ZeroLengthOuterEdgeDoesNotAdmitAnOutsideCenter()
    {
        Vector2 repeated = new(0, 0);
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            repeated, repeated, new Vector2(8, 8), new Vector2(0, 8), out var frame));

        Assert.True(CenterInside(frame.Map(new Vector2(1, 6))));
        Assert.False(CenterInside(frame.Map(new Vector2(7, 2))));
        Assert.True(AllFinite(frame.Map(repeated)));
    }

    [Fact]
    public void GeometricEdgePayloadDistinguishesExternalBoundaryFromInternalDiagonal()
    {
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(8, 0),
            new Vector2(8, 8), new Vector2(0, 8), out var rectangle));
        Assert.True(PreservesOrdinaryCenter(rectangle.Map(new Vector2(4, 4))));
        Assert.False(PreservesOrdinaryCenter(rectangle.Map(new Vector2(0, 4))));
        Assert.False(PreservesOrdinaryCenter(rectangle.Map(new Vector2(0, 0))));
        Assert.True(PreservesOrdinaryCenter(rectangle.Map(new Vector2(4, 3))));

        // Both triangles lie above their shared q0-q2 edge, so that edge is
        // part of the union's exterior, not an internal triangulation seam.
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(4, 8),
            new Vector2(8, 0), new Vector2(8, 8), out var overlapping));
        Assert.False(PreservesOrdinaryCenter(overlapping.Map(new Vector2(4, 0))));
        // An outer edge of the first triangle lies inside the second one.
        Assert.True(PreservesOrdinaryCenter(overlapping.Map(new Vector2(6, 4))));

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(8, 8),
            new Vector2(8, 0), new Vector2(0, 8), out var crossing));
        Assert.False(PreservesOrdinaryCenter(crossing.Map(new Vector2(4, 0))));
    }

    [Fact]
    public void CollapsedConstituentMakesFormerSharedEdgeExternal()
    {
        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(4, 0),
            new Vector2(8, 0), new Vector2(0, 8), out var firstCollapsed));
        Assert.False(PreservesOrdinaryCenter(firstCollapsed.Map(new Vector2(4, 0))));
        Assert.True(PreservesOrdinaryCenter(firstCollapsed.Map(new Vector2(2, 2))));

        Assert.True(SdlGpuImageDomainGeometry.TryCreate(
            new Vector2(0, 0), new Vector2(8, 0),
            new Vector2(8, 8), new Vector2(4, 4), out var secondCollapsed));
        Assert.False(PreservesOrdinaryCenter(secondCollapsed.Map(new Vector2(4, 4))));
        Assert.True(PreservesOrdinaryCenter(secondCollapsed.Map(new Vector2(6, 2))));
    }

    private static bool AllFinite(SdlGpuImageDomainEdges value) =>
        float.IsFinite(value.E01) && float.IsFinite(value.E12) &&
        float.IsFinite(value.E02) && float.IsFinite(value.E23) &&
        float.IsFinite(value.E30);

    private static bool CenterInside(SdlGpuImageDomainEdges value) =>
        SameSign(value.E01, value.E12, -value.E02) ||
        SameSign(value.E02, value.E23, value.E30);

    private static bool SameSign(float a, float b, float c) =>
        (a >= 0 && b >= 0 && c >= 0) ||
        (a <= 0 && b <= 0 && c <= 0);

    // Mirrors the fragment predicate's *intended* logical-union semantics;
    // native readback tests verify the compiled shader at physical pixel centers.
    private static bool PreservesOrdinaryCenter(SdlGpuImageDomainEdges edges)
    {
        bool firstPositive = edges.E01 > 0 && edges.E12 > 0;
        bool firstNegative = edges.E01 < 0 && edges.E12 < 0;
        bool secondPositive = edges.E23 > 0 && edges.E30 > 0;
        bool secondNegative = edges.E23 < 0 && edges.E30 < 0;
        return (edges.E02 < 0 && firstPositive) ||
            (edges.E02 > 0 && firstNegative) ||
            (edges.E02 > 0 && secondPositive) ||
            (edges.E02 < 0 && secondNegative) ||
            (edges.E02 == 0 &&
                ((firstPositive && secondPositive) ||
                 (firstNegative && secondNegative)));
    }

}
