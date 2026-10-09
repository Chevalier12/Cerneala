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

    [Theory]
    [InlineData(0.34f, 1.56f, 0.64f, 1f, 0.4925f, 1.085f)]
    [InlineData(0.36f, 0f, 0.66f, -0.56f, 0.5075f, -0.085f)]
    public void CubicBezierPreservesOvershootAndUndershoot(
        float x1, float y1, float x2, float y2, float progress, float expected)
    {
        CubicBezierEasing easing = new(x1, y1, x2, y2);

        // These inputs are x(t) at t = 0.5; the expected values are y(t).
        Assert.Equal(expected, easing.Transform(progress), precision: 5);
        Assert.Equal(0, easing.Transform(0));
        Assert.Equal(1, easing.Transform(1));
    }

    [Fact]
    public void FloatTweenWithEaseOutBackOvershootsAndFinishesAtExactTarget()
    {
        TweenSpec<float> spec = new(TimeSpan.FromSeconds(1), new CubicBezierEasing(0.34f, 1.56f, 0.64f, 1));
        MotionSpecContext context = new(
            Cerneala.UI.Motion.Core.ReducedMotionPolicy.Default,
            new Cerneala.UI.Motion.Interpolation.ValueMixerRegistry(),
            Diagnostics: null,
            Now: TimeSpan.Zero);
        MotionSampler<float> sampler = spec.CreateSampler(
            0, 100, new Cerneala.UI.Motion.Interpolation.FloatMixer(), context);

        sampler.Advance(TimeSpan.FromMilliseconds(492.5));

        Assert.Equal(108.5f, sampler.Current, precision: 3);
        Assert.False(sampler.IsComplete);

        sampler.Advance(TimeSpan.FromMilliseconds(507.5));

        Assert.Equal(100, sampler.Current);
        Assert.True(sampler.IsComplete);
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
