using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.Tests.SdlGpu;

public sealed class RenderSurface3DMathOracleTests
{
    [Fact]
    public void AsymmetricCameraProjectsFrozenWorldPoints()
    {
        foreach (RenderSurface3DStageZeroOracle.ProjectionCase expected in
            RenderSurface3DStageZeroOracle.ProjectionCases)
        {
            RenderSurface3DStageZeroOracle.ProjectionResult actual =
                RenderSurface3DStageZeroOracle.Project(expected.World);

            Assert.True(actual.IsInsideClipVolume);
            AssertClose(expected.ExpectedPixel, actual.Pixel, 0.001f);
            Assert.InRange(MathF.Abs(actual.Depth - expected.ExpectedDepth), 0, 0.000001f);
        }
    }

    [Fact]
    public void CrossingScreenLinesKeepDistinctDepth()
    {
        RenderSurface3DStageZeroOracle.CrossingLinesScenario scenario =
            RenderSurface3DStageZeroOracle.CrossingLines;
        RenderSurface3DStageZeroOracle.ProjectionResult horizontalStart =
            RenderSurface3DStageZeroOracle.Project(scenario.HorizontalStart);
        RenderSurface3DStageZeroOracle.ProjectionResult horizontalEnd =
            RenderSurface3DStageZeroOracle.Project(scenario.HorizontalEnd);
        RenderSurface3DStageZeroOracle.ProjectionResult verticalStart =
            RenderSurface3DStageZeroOracle.Project(scenario.VerticalStart);
        RenderSurface3DStageZeroOracle.ProjectionResult verticalEnd =
            RenderSurface3DStageZeroOracle.Project(scenario.VerticalEnd);

        AssertClose(scenario.ExpectedHorizontalStartPixel, horizontalStart.Pixel, 0.001f);
        AssertClose(scenario.ExpectedHorizontalEndPixel, horizontalEnd.Pixel, 0.001f);
        AssertClose(scenario.ExpectedVerticalStartPixel, verticalStart.Pixel, 0.001f);
        AssertClose(scenario.ExpectedVerticalEndPixel, verticalEnd.Pixel, 0.001f);
        Assert.InRange(MathF.Abs(horizontalStart.Depth - scenario.ExpectedHorizontalDepth), 0, 0.000001f);
        Assert.InRange(MathF.Abs(verticalStart.Depth - scenario.ExpectedVerticalDepth), 0, 0.000001f);
        Assert.True(
            horizontalStart.Depth < verticalStart.Depth,
            "The line at view z=-3 must win depth over the line at view z=-6.");
    }

    [Fact]
    public void SegmentCrossingNearPlaneIsClippedBeforePerspectiveDivide()
    {
        RenderSurface3DStageZeroOracle.NearPlaneScenario scenario =
            RenderSurface3DStageZeroOracle.NearPlaneCrossing;

        Assert.True(RenderSurface3DStageZeroOracle.ClipToNearPlane(
            scenario.Start,
            scenario.End,
            out Vector3 clippedStart,
            out Vector3 clippedEnd));

        AssertClose(scenario.ExpectedClippedStart, clippedStart, 0.00001f);
        AssertClose(scenario.End, clippedEnd, 0.00001f);
        Vector3 clippedView = Vector3.Transform(clippedStart, RenderSurface3DStageZeroOracle.View);
        Assert.Equal(-RenderSurface3DStageZeroOracle.NearPlane, clippedView.Z, 5);
        RenderSurface3DStageZeroOracle.ProjectionResult clippedProjection =
            RenderSurface3DStageZeroOracle.Project(clippedStart);
        Assert.InRange(clippedProjection.Depth, -0.00001f, 0.00001f);
        Assert.True(float.IsFinite(clippedProjection.Pixel.X));
        Assert.True(float.IsFinite(clippedProjection.Pixel.Y));
    }

    [Fact]
    public void AnalyticMasksFreezeButtLineAndDiscBoundaries()
    {
        RenderSurface3DStageZeroOracle.PrimitivePixelMask line =
            RenderSurface3DStageZeroOracle.CreateButtLineMask(
                64,
                48,
                new Vector2(8.25f, 12.25f),
                new Vector2(40.75f, 12.25f),
                5.5f);
        RenderSurface3DStageZeroOracle.PrimitivePixelMask disc =
            RenderSurface3DStageZeroOracle.CreateDiscMask(
                64,
                48,
                new Vector2(25.25f, 22.75f),
                12.8f);

        Assert.Equal(PixelMaskClassification.OpaqueInterior, line[20, 12].Classification);
        Assert.Equal(PixelMaskClassification.BoundaryBand, line[7, 12].Classification);
        Assert.Equal(PixelMaskClassification.BoundaryBand, line[41, 12].Classification);
        Assert.Equal(PixelMaskClassification.Exterior, line[5, 12].Classification);
        Assert.Equal(PixelMaskClassification.OpaqueInterior, disc[25, 22].Classification);
        Assert.Equal(PixelMaskClassification.BoundaryBand, disc[31, 22].Classification);
        Assert.Equal(PixelMaskClassification.Exterior, disc[34, 22].Classification);

        foreach (PixelMaskClassification classification in Enum.GetValues<PixelMaskClassification>())
        {
            Assert.Contains(line.Samples, sample => sample.Classification == classification);
            Assert.Contains(disc.Samples, sample => sample.Classification == classification);
        }
    }

    [Fact]
    public void CoverageCriteriaAcceptAnalyticReferenceForLineAndDisc()
    {
        RenderSurface3DStageZeroOracle.PrimitivePixelMask line =
            RenderSurface3DStageZeroOracle.CreateButtLineMask(
                64,
                48,
                new Vector2(8.25f, 12.25f),
                new Vector2(40.75f, 12.25f),
                5.5f);
        RenderSurface3DStageZeroOracle.PrimitivePixelMask disc =
            RenderSurface3DStageZeroOracle.CreateDiscMask(
                64,
                48,
                new Vector2(25.25f, 22.75f),
                12.8f);

        AssertPassesCoverageContract(line);
        AssertPassesCoverageContract(disc);
    }

    [Fact]
    public void PixelAlignedCoverageDoesNotRequireFractionalBoundarySamples()
    {
        RenderSurface3DStageZeroOracle.PrimitivePixelMask line =
            RenderSurface3DStageZeroOracle.CreateButtLineMask(
                64,
                48,
                new Vector2(8, 12),
                new Vector2(40, 12),
                6);

        RenderSurface3DStageZeroOracle.MaskCoverageResult result =
            RenderSurface3DStageZeroOracle.EvaluateCoverage(
                line,
                CreateAnalyticReference(line),
                Color.White,
                Color.Black,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.PixelAlignedUnoccluded);

        Assert.True(result.Passes);
        Assert.Equal(0, result.BoundaryPartialSampleCount);
        Assert.False(result.Expectations.RequirePartialBoundaryCoverage);
        Assert.True(result.ReachesAnalyticExtents);
    }

    [Fact]
    public void CoverageCriteriaRejectExteriorHaloAndInsetExtent()
    {
        RenderSurface3DStageZeroOracle.PrimitivePixelMask disc =
            RenderSurface3DStageZeroOracle.CreateDiscMask(
                64,
                48,
                new Vector2(25.25f, 22.75f),
                12.8f);

        Color[] halo = CreateAnalyticReference(disc);
        int exterior = Array.FindIndex(
            disc.Samples,
            sample => sample.Classification == PixelMaskClassification.Exterior);
        halo[exterior] = Color.White;
        RenderSurface3DStageZeroOracle.MaskCoverageResult haloResult =
            RenderSurface3DStageZeroOracle.EvaluateCoverage(
                disc,
                halo,
                Color.White,
                Color.Black,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.FractionalPhaseUnoccluded);
        Assert.False(haloResult.Passes);
        Assert.Equal(1, haloResult.ExteriorHaloSampleCount);

        Color[] inset = CreateAnalyticReference(disc);
        for (int index = 0; index < inset.Length; index++)
        {
            float centerX = (index % disc.Width) + 0.5f;
            if (centerX > disc.AnalyticBounds.Right - RenderSurface3DStageZeroOracle.AntialiasingBandPixels)
            {
                inset[index] = Color.Black;
            }
        }
        RenderSurface3DStageZeroOracle.MaskCoverageResult insetResult =
            RenderSurface3DStageZeroOracle.EvaluateCoverage(
                disc,
                inset,
                Color.White,
                Color.Black,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.FractionalPhaseUnoccluded);
        Assert.False(insetResult.Passes);
        Assert.False(insetResult.ReachesAnalyticExtents);
        Assert.Equal(0, insetResult.ExteriorHaloSampleCount);
    }

    [Fact]
    public void FrozenVisualThresholdsRemainExplicit()
    {
        Assert.Equal(1, RenderSurface3DStageZeroOracle.ProjectionPixelTolerance);
        Assert.Equal((byte)1, RenderSurface3DStageZeroOracle.OpaqueInteriorChannelTolerance);
        Assert.Equal(2, RenderSurface3DStageZeroOracle.AntialiasingBandPixels);
    }

    private static void AssertPassesCoverageContract(
        RenderSurface3DStageZeroOracle.PrimitivePixelMask mask)
    {
        RenderSurface3DStageZeroOracle.MaskCoverageResult result =
            RenderSurface3DStageZeroOracle.EvaluateCoverage(
                mask,
                CreateAnalyticReference(mask),
                Color.White,
                Color.Black,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.FractionalPhaseUnoccluded);

        Assert.True(result.Passes);
        Assert.Equal(0, result.OpaqueInteriorViolationCount);
        Assert.True(result.BoundaryCoveredSampleCount > 0);
        Assert.True(result.BoundaryPartialSampleCount > 0);
        Assert.Equal(0, result.ExteriorHaloSampleCount);
        Assert.True(result.ReachesAnalyticExtents);
        Assert.True(result.StaysInsideBoundaryBand);
    }

    private static Color[] CreateAnalyticReference(
        RenderSurface3DStageZeroOracle.PrimitivePixelMask mask)
    {
        Color[] pixels = new Color[mask.Samples.Length];
        for (int index = 0; index < pixels.Length; index++)
        {
            float coverage = Math.Clamp(0.5f - mask.Samples[index].SignedDistance, 0, 1);
            byte value = (byte)MathF.Round(coverage * byte.MaxValue);
            pixels[index] = new Color(value, value, value, byte.MaxValue);
        }
        return pixels;
    }

    private static void AssertClose(Vector2 expected, Vector2 actual, float tolerance) =>
        Assert.True(
            Vector2.Distance(expected, actual) <= tolerance,
            $"Expected {expected}, actual {actual}, tolerance {tolerance}.");

    private static void AssertClose(Vector3 expected, Vector3 actual, float tolerance) =>
        Assert.True(
            Vector3.Distance(expected, actual) <= tolerance,
            $"Expected {expected}, actual {actual}, tolerance {tolerance}.");
}
