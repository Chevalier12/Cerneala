using Cerneala.UI.Motion.Input;

namespace Cerneala.Tests.UI.Motion.Input;

public sealed class MotionRangeTests
{
    [Theory]
    [InlineData(-1f, 1f, 0f, 1f, 0f, 0.5f)]
    [InlineData(-1f, 1f, 0f, 1f, 1f, 1f)]
    [InlineData(-3e38f, 3e38f, 0f, 1f, 0f, 0.5f)]
    [InlineData(-3e38f, 3e38f, 0f, 1f, 3e38f, 1f)]
    public void MapIsRightForFiniteInputIntervalsWhoseWidthOverflowsFloat(
        float inputStart, float inputEnd, float outputStart, float outputEnd, float value, float expected)
    {
        MotionRange range = new(inputStart, inputEnd, outputStart, outputEnd);

        float actual = range.Map(value);

        Assert.True(
            Math.Abs(actual - expected) < 1e-4f,
            $"({inputStart}, {inputEnd}) -> ({outputStart}, {outputEnd}), Map({value}) = {actual}, expected {expected}.");
    }

    [Theory]
    [InlineData(0f, 1f, -1f, 1f, 0f, -1f)]
    [InlineData(0f, 1f, -1f, 1f, 0.5f, 0f)]
    [InlineData(0f, 1f, -3e38f, 3e38f, 0f, -3e38f)]
    [InlineData(0f, 1f, -3e38f, 3e38f, 0.5f, 0f)]
    [InlineData(0f, 1f, -3e38f, 3e38f, 1f, 3e38f)]
    public void MapIsRightForFiniteOutputIntervalsWhoseWidthOverflowsFloat(
        float inputStart, float inputEnd, float outputStart, float outputEnd, float value, float expected)
    {
        MotionRange range = new(inputStart, inputEnd, outputStart, outputEnd);

        float actual = range.Map(value);

        Assert.True(
            Math.Abs(actual - expected) <= 1e-6f * Math.Max(1f, Math.Abs(expected)),
            $"({inputStart}, {inputEnd}) -> ({outputStart}, {outputEnd}), Map({value}) = {actual}, expected {expected}.");
    }
}
