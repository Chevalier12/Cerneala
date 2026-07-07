using Cerneala.UI.Animation;
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

    [Fact]
    public void LegacyEasingLinearRemainsSafe()
    {
        Assert.Equal(0, Easing.Linear(float.NaN));
        Assert.Equal(0.5f, Easing.Linear(0.5f));
        Assert.Equal(1, Easing.Linear(2));
    }
}
