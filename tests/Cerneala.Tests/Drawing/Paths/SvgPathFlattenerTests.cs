using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Paths;
using Microsoft.Xna.Framework;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.Paths;

public sealed class SvgPathFlattenerTests
{
    [Fact]
    public void FlattensAbsoluteAndRelativeLineCommands()
    {
        IReadOnlyList<DrawPoint[]> contours = SvgPathFlattener.Flatten(
            "M1 2h10v8l-10 0z",
            0.1f);

        DrawPoint[] contour = Assert.Single(contours);
        Assert.Equal(
            [
                new DrawPoint(1, 2),
                new DrawPoint(11, 2),
                new DrawPoint(11, 10),
                new DrawPoint(1, 10)
            ],
            contour);
    }

    [Fact]
    public void FlattensCurvesAndArcCommands()
    {
        IReadOnlyList<DrawPoint[]> contours = SvgPathFlattener.Flatten(
            "M0 0C10 0 10 10 20 10S30 20 40 10Q45 0 50 10T60 10A5 5 0 0 1 70 10Z",
            0.1f);

        DrawPoint[] contour = Assert.Single(contours);
        Assert.True(contour.Length > 12);
        Assert.Equal(new DrawPoint(0, 0), contour[0]);
        Assert.Equal(new DrawPoint(70, 10), contour[^1]);
    }

    [Fact]
    public void TessellatesConcaveSvgPathIntoMonoGameTriangles()
    {
        MonoGamePathMesh mesh = MonoGamePathMeshBuilder.Build(
            "M0 0L10 0L10 10L5 5L0 10Z",
            new DrawRect(0, 0, 10, 10),
            100,
            100,
            0,
            0,
            XnaColor.White);

        Assert.False(mesh.IsEmpty);
        Assert.Equal(0, mesh.Indices.Length % 3);
        Assert.All(mesh.Indices, index => Assert.InRange(index, 0, mesh.Vertices.Length - 1));
    }

    [Theory]
    [InlineData("L1 1")]
    [InlineData("M0 0C1 2")]
    [InlineData("M0 0A1 1 0 2 0 3 3")]
    public void RejectsMalformedPathData(string data)
    {
        Assert.Throws<FormatException>(() => SvgPathFlattener.Flatten(data, 0.1f));
    }
}
