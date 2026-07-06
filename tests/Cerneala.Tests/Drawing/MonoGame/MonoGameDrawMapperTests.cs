using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class MonoGameDrawMapperTests
{
    [Fact]
    public void ScaledRectangleMapsLogicalBoundsToPhysicalPixels()
    {
        MonoGameDrawMapper mapper = new(2);

        Rectangle rectangle = mapper.MapRectangle(new DrawRect(0.3f, 0.3f, 1.4f, 1.4f));

        Assert.Equal(new Rectangle(1, 1, 2, 2), rectangle);
    }

    [Fact]
    public void ScaledVectorMapsLogicalPointToPhysicalVector()
    {
        MonoGameDrawMapper mapper = new(2);

        Vector2 vector = mapper.MapVector(new DrawPoint(10.5f, 20.25f));

        Assert.Equal(new Vector2(21, 40.5f), vector);
    }

    [Fact]
    public void ScaledThicknessRoundsToAtLeastOnePhysicalPixel()
    {
        MonoGameDrawMapper mapper = new(2);

        Assert.Equal(1, mapper.MapThickness(0.2f));
        Assert.Equal(3, mapper.MapThickness(1.25f));
    }

    [Fact]
    public void ScaleOnePreservesExistingMapping()
    {
        MonoGameDrawMapper mapper = new(1);

        Assert.Equal(new Rectangle(10, 20, 30, 40), mapper.MapRectangle(new DrawRect(10, 20, 30, 40)));
        Assert.Equal(new Vector2(10, 20), mapper.MapVector(new DrawPoint(10, 20)));
        Assert.Equal(2, mapper.MapThickness(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidScaleIsRejected(float scale)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonoGameDrawMapper(scale));
    }
}
