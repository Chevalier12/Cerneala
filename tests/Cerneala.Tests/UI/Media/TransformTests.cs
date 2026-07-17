using Cerneala.Drawing;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Media;

public sealed class TransformTests
{
    [Fact]
    public void IdentityTransformLeavesPointUnchanged()
    {
        DrawPoint point = new(3, 4);

        Assert.Equal(point, Transform.Identity.Apply(point));
    }

    [Fact]
    public void MatrixTransformAppliesTranslation()
    {
        Transform transform = new(Matrix3x2.CreateTranslation(10, 20));

        Assert.Equal(new DrawPoint(11, 22), transform.Apply(new DrawPoint(1, 2)));
    }

    [Fact]
    public void TransformCompositionIsDeterministic()
    {
        Transform scale = new(Matrix3x2.CreateScale(2, 3));
        Transform translate = new(Matrix3x2.CreateTranslation(10, 20));

        Transform composed = scale.Compose(translate);

        Assert.Equal(new DrawPoint(12, 26), composed.Apply(new DrawPoint(1, 2)));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void MatrixRejectsNonFiniteValues(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Matrix3x2(value, 0, 0, 1, 0, 0));
    }

}
