using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class SceneEntityBoundsTests
{
    [Fact]
    public void PointUsesPositionNotSizePivotOrTemplateGeometry()
    {
        Scene2DEntity entity = new("marker", "map", new(10, -20), new(80, 90),
            rotation: 1.7f, pivot: new(0.5f, 1), role: "Spawn");
        Assert.Equal(new DrawRect(10, -20, 0, 0), entity.GetAuthoringBounds());
        Assert.Null(entity.GetCollisionBounds());
    }

    [Fact]
    public void BoxAppliesRotationThenMapLocalTranslationWithoutPivot()
    {
        Scene2DEntity entity = new("box", "map", new(100, 50), new(10, 20),
            shape: "Box", rotation: MathF.PI / 2, pivot: new(1, 1));
        AssertBounds(new(80, 50, 20, 10), entity.GetAuthoringBounds());
    }

    [Fact]
    public void PolygonBoundsComeFromRotatedVerticesNotTheSizeMetadata()
    {
        Scene2DEntity entity = new("polygon", "map", new(30, 10), new(100, 100),
            shape: "Polygon", points: "0,0 8,0 0,4", rotation: MathF.PI / 2);
        AssertBounds(new(26, 10, 4, 8), entity.GetAuthoringBounds());
    }

    [Fact]
    public void OpenNonconvexPolylineUsesEveryVertex()
    {
        Scene2DEntity entity = new("line", "map", new(30, 10), default,
            shape: "Polyline", points: "-3,1 2,-2 1,5", rotation: MathF.PI / 2);
        AssertBounds(new(25, 7, 7, 5), entity.GetAuthoringBounds());
        Assert.Equal(3, entity.Vertices.Count);
    }

    [Fact]
    public void EllipseUsesItsExactRotatedExtentsNotTheRotatedBox()
    {
        Scene2DEntity entity = new("ellipse", "map", new(10, 20), new(8, 4),
            shape: "Ellipse", rotation: MathF.PI / 4);
        float extent = MathF.Sqrt(10);
        float centerX = 10 + MathF.Sqrt(2), centerY = 20 + 3 * MathF.Sqrt(2);
        AssertBounds(new(centerX - extent, centerY - extent, extent * 2, extent * 2), entity.GetAuthoringBounds());
    }

    [Fact]
    public void CollisionEnvelopeIsIndependentAndKeepsOffsetAndAffineEllipse()
    {
        TileColliderDescriptor2D collider = new(TileColliderShape2D.Circle,
            Matrix3x2.CreateScale(3, 1), radius: 2, offsetX: 1, offsetY: 2);
        Scene2DEntity entity = new("entity", "map", new(10, 20), default,
            rotation: MathF.PI / 2, pivot: new(1, 1), collider: collider);
        Assert.Equal(new DrawRect(10, 20, 0, 0), entity.GetAuthoringBounds());
        AssertBounds(new(6, 17, 4, 12), entity.GetCollisionBounds()!.Value);
    }

    [Theory]
    [InlineData(0, 2_000_000_128f)]
    [InlineData(1_500_000_000f, 1_000_000_000f)]
    public void AuthoringBoundsPreserveTheExistingDrawRectRangeValidation(float x, float width)
    {
        // DrawPoint/DrawSize accept these finite components, but DrawRect also
        // bounds sizes and edges to its documented drawing coordinate range.
        Scene2DEntity entity = new("box", "map", new(x, 0), new(width, 1), shape: "Box");
        Assert.Throws<ArgumentOutOfRangeException>(() => entity.GetAuthoringBounds());
        Assert.Null(entity.GetCollisionBounds());
    }

    [Fact]
    public void NonfiniteTransformedEllipseExtentsDoNotBecomeAnEmptyRegion()
    {
        Scene2DEntity entity = new("ellipse", "map", default, new(float.MaxValue, 1), shape: "Ellipse");
        Assert.Throws<InvalidOperationException>(() => entity.GetAuthoringBounds());
    }

    private static void AssertBounds(DrawRect expected, DrawRect actual)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.Width - actual.Width), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.Height - actual.Height), 0, 0.0001f);
    }
}
