using Cerneala.UI.Motion.Specs;

namespace Cerneala.Tests.UI.Motion.Specs;

public sealed class EasingTests
{
    [Fact]
    public void CubicBezierEndpointsAreExact()
    {
        CubicBezierEasing easing = new(0.4f, 0, 0.2f, 1);

        Assert.Equal(0, easing.Transform(0));
        Assert.Equal(1, easing.Transform(1));
    }

    [Fact]
    public void CubicBezierIsMonotonicForValidCurve()
    {
        CubicBezierEasing easing = new(0.4f, 0, 0.2f, 1);
        float previous = 0;

        for (int i = 1; i <= 100; i++)
        {
            float current = easing.Transform(i / 100f);
            Assert.True(current >= previous, $"{current} should be >= {previous} at step {i}");
            previous = current;
        }
    }

    [Theory]
    [InlineData(float.NaN, 1)]
    [InlineData(float.PositiveInfinity, 1)]
    [InlineData(float.NegativeInfinity, 1)]
    [InlineData(0, float.NaN)]
    [InlineData(0, float.PositiveInfinity)]
    [InlineData(0, float.NegativeInfinity)]
    public void CubicBezierRejectsNaNAndInfinityYControlPoints(float y1, float y2)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CubicBezierEasing(0.4f, y1, 0.2f, y2));
    }

    [Fact]
    public void StepEasingMatchesJumpModes()
    {
        Assert.Equal(0.25f, new StepEasing(4, StepPosition.JumpStart).Transform(0));
        Assert.Equal(0, new StepEasing(4, StepPosition.JumpEnd).Transform(0.24f));
        Assert.Equal(0.25f, new StepEasing(4, StepPosition.JumpEnd).Transform(0.25f));
        Assert.Equal(0.2f, new StepEasing(4, StepPosition.JumpBoth).Transform(0));
        Assert.Equal(0, new StepEasing(4, StepPosition.JumpNone).Transform(0.24f));
        Assert.Equal(1f / 3f, new StepEasing(4, StepPosition.JumpNone).Transform(0.25f), precision: 3);
    }

}
