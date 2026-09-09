using Cerneala.Drawing;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Media;

public sealed class RichPathGeometryTests
{
    [Fact]
    public void FromPathRetainsTheImmutablePathAndExposesOnlyItsAnchorsAsPoints()
    {
        DrawPath path = new DrawPathBuilder()
            .MoveTo(new DrawPoint(0, 0))
            .QuadraticTo(new DrawPoint(10, 20), new DrawPoint(20, 0))
            .Close()
            .MoveTo(new DrawPoint(3, 4))
            .LineTo(new DrawPoint(5, 6))
            .Build();

        PathGeometry geometry = PathGeometry.FromPath(path);

        Assert.Same(path, geometry.Path);
        Assert.Equal(path.Bounds, geometry.Bounds);
        Assert.Equal([new DrawPoint(0, 0), new DrawPoint(20, 0), new DrawPoint(3, 4), new DrawPoint(5, 6)], geometry.Points);
        Assert.Throws<NotSupportedException>(() => ((IList<DrawPoint>)geometry.Points)[0] = new DrawPoint(1, 1));
    }

    [Fact]
    public void LegacySinglePointKeepsItsBoundsWithoutManufacturingADrawableContour()
    {
        PathGeometry geometry = new([new DrawPoint(2, 3)]);
        Assert.Null(geometry.Path);
        Assert.Equal(new DrawRect(2, 3, 0, 0), geometry.Bounds);
        Assert.Equal(new DrawPoint(2, 3), Assert.Single(geometry.Points));
    }

    [Fact]
    public void SvgParsingUsesTheExistingParserAndRejectsInvalidData()
    {
        const string data = "m0 0 h10 v10 q5 5 10 0 t10 0 c5 0 5 5 10 5 s5 -5 10 -5 a5 3 30 0 1 10 0 z";
        DrawPath expected = DrawPathParser.ParseSvg(data);
        PathGeometry geometry = PathGeometry.Parse(data);
        Assert.Equal(expected.Bounds, geometry.Bounds);
        Assert.Equal(expected.Contours[0].Segments, geometry.Path!.Contours[0].Segments);
        Assert.Throws<FormatException>(() => PathGeometry.Parse("M0 0 Lnope"));
        Assert.ThrowsAny<ArgumentException>(() => PathGeometry.Parse(" "));
        Assert.Throws<ArgumentNullException>(() => PathGeometry.FromPath(null!));
    }
}
