using Xunit.Sdk;

namespace Cerneala.Tests.Timbre.Harness;

public sealed class TimbreRigTests
{
    [Fact]
    public void AssertPcmRejectsANaNActualSampleAndNamesIt()
    {
        XunitException failure = Assert.ThrowsAny<XunitException>(() => TimbreRig.AssertPcm([0.25f, 0.25f], [0.25f, float.NaN], 0f));

        Assert.Contains("sample 1", failure.Message);
        Assert.Contains($"expected {0.25f}", failure.Message);
        Assert.Contains($"actual {float.NaN}", failure.Message);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void AssertPcmRejectsANonFiniteActualSampleAgainstAFiniteExpectedSample(float actual)
    {
        Assert.ThrowsAny<XunitException>(() => TimbreRig.AssertPcm([0.25f], [actual], 1f));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void AssertPcmRejectsAFiniteActualSampleAgainstANonFiniteExpectedSample(float expected)
    {
        Assert.ThrowsAny<XunitException>(() => TimbreRig.AssertPcm([expected], [0.25f], 1f));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void AssertPcmAcceptsAnIdenticalNonFiniteSample(float sample)
    {
        TimbreRig.AssertPcm([sample], [sample], 0f);
    }

    [Fact]
    public void AssertPcmRejectsOppositeInfinities()
    {
        Assert.ThrowsAny<XunitException>(() => TimbreRig.AssertPcm([float.PositiveInfinity], [float.NegativeInfinity], 1f));
    }

    [Fact]
    public void AssertPcmAcceptsFiniteSamplesWithinTolerance()
    {
        TimbreRig.AssertPcm([0.25f, -0.5f], [0.2501f, -0.4999f], 1e-3f);
    }
}
