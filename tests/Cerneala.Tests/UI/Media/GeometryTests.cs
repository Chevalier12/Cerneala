using Cerneala.Drawing;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Media;

public sealed class GeometryTests
{
    [Fact]
    public void RectangleGeometryExposesBounds()
    {
        DrawRect rect = new(1, 2, 30, 40);

        RectangleGeometry geometry = new(rect);

        Assert.Equal(rect, geometry.Bounds);
    }

    [Fact]
    public void EllipseGeometryExposesBounds()
    {
        DrawRect rect = new(1, 2, 30, 40);

        EllipseGeometry geometry = new(rect);

        Assert.Equal(rect, geometry.Bounds);
    }

    [Fact]
    public void PathGeometryCalculatesBoundsFromStructuredPoints()
    {
        PathGeometry geometry = new(
        [
            new DrawPoint(10, 20),
            new DrawPoint(2, 40),
            new DrawPoint(30, 5)
        ]);

        Assert.Equal(new DrawRect(2, 5, 28, 35), geometry.Bounds);
        Assert.Equal(3, geometry.Points.Count);
    }

    [Fact]
    public void PathGeometryExposesImmutablePointData()
    {
        PathGeometry geometry = new(
        [
            new DrawPoint(1, 2),
            new DrawPoint(3, 4)
        ]);

        IList<DrawPoint> exposedPoints = Assert.IsAssignableFrom<IList<DrawPoint>>(geometry.Points);

        Assert.Throws<NotSupportedException>(() => exposedPoints[0] = new DrawPoint(10, 20));
        Assert.Equal(new DrawPoint(1, 2), geometry.Points[0]);
    }

    [Fact]
    public void PathGeometryRejectsEmptyPointList()
    {
        Assert.Throws<ArgumentException>(() => new PathGeometry([]));
    }
}
