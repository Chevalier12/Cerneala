using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

public sealed class PrismBlendMathMigrationTests
{
    [Theory]
    [InlineData(0.2, 0)]
    [InlineData(0.3, 0.5)]
    [InlineData(0.5, 1)]
    [InlineData(0.7, 0.5)]
    [InlineData(0.8, 0)]
    public void BlendIfUsesLinearSplitFeathers(double value, double expected)
    {
        double actual = PrismBlendMath.EvaluateBlendRange(value, new PrismBlendRange(0.2f, 0.4f, 0.6f, 0.8f));
        Assert.Equal(expected, actual, precision: 6);
    }

    [Theory]
    [InlineData(PrismBlendMode.Multiply, 0.2, 0.2, 0.15)]
    [InlineData(PrismBlendMode.Screen, 0.85, 0.7, 0.8)]
    [InlineData(PrismBlendMode.Difference, 0.55, 0.1, 0.55)]
    public void OpaqueBlendSentinelsMatchKnownChannelEquations(PrismBlendMode mode, double red, double green, double blue)
    {
        PrismPremultipliedColor actual = PrismBlendMath.Composite(mode,
            new(0.8, 0.4, 0.2, 1), new(0.25, 0.5, 0.75, 1), PrismBlendOptions.Default);
        Assert.InRange(Math.Abs(actual.Red - red), 0, 0.0000001);
        Assert.InRange(Math.Abs(actual.Green - green), 0, 0.0000001);
        Assert.InRange(Math.Abs(actual.Blue - blue), 0, 0.0000001);
        Assert.InRange(Math.Abs(actual.Alpha - 1), 0, 0.0000001);
    }
}
