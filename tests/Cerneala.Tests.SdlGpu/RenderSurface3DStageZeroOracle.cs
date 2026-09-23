using System.Numerics;
using Cerneala.Drawing;

namespace Cerneala.Tests.SdlGpu;

// Test-only oracle shared by the Stage 0 contract tests and later native 3D
// conformance. It deliberately does not call production projection or raster helpers.
internal static class RenderSurface3DStageZeroOracle
{
    internal const int RasterWidth = 320;
    internal const int RasterHeight = 180;
    internal const float ProjectionPixelTolerance = 1;
    internal const byte OpaqueInteriorChannelTolerance = 1;
    internal const int AntialiasingBandPixels = 2;
    internal const float NearPlane = 0.01f;

    private static readonly ProjectionCase[] FrozenProjectionCases =
    [
        new(new Vector3(0.5f, -0.25f, 0), new Vector2(160, 90), 0.9987223f),
        new(Vector3.Zero, new Vector2(150.65501f, 84.28431f), 0.99873686f),
        new(new Vector3(1.25f, 0.5f, -2), new Vector2(183.59598f, 69.63024f), 0.99891233f),
        new(new Vector3(-1, 1, 1.5f), new Vector2(114.268074f, 67.73676f), 0.99847966f)
    ];

    internal static Vector3 Eye => new(3, 2, 7);

    internal static Vector3 Target => new(0.5f, -0.25f, 0);

    internal static Matrix4x4 View =>
        Matrix4x4.CreateLookAt(Eye, Target, Vector3.UnitY);

    internal static Matrix4x4 Projection =>
        Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 3,
            RasterWidth / (float)RasterHeight,
            NearPlane,
            1000);

    internal static ReadOnlySpan<ProjectionCase> ProjectionCases =>
        FrozenProjectionCases;

    internal static CrossingLinesScenario CrossingLines
    {
        get
        {
            Matrix4x4.Invert(View, out Matrix4x4 inverseView);
            return new CrossingLinesScenario(
                Vector3.Transform(new Vector3(-1, 0, -3), inverseView),
                Vector3.Transform(new Vector3(1, 0, -3), inverseView),
                Vector3.Transform(new Vector3(0, -1, -6), inverseView),
                Vector3.Transform(new Vector3(0, 1, -6), inverseView),
                new Vector2(108.038475f, 90),
                new Vector2(211.96153f, 90),
                new Vector2(160, 115.98077f),
                new Vector2(160, 64.01924f),
                0.99667674f,
                0.9983433f);
        }
    }

    internal static NearPlaneScenario NearPlaneCrossing
    {
        get
        {
            Matrix4x4.Invert(View, out Matrix4x4 inverseView);
            return new NearPlaneScenario(
                Vector3.Transform(new Vector3(-0.4f, 0.2f, -0.005f), inverseView),
                Vector3.Transform(new Vector3(0.6f, -0.3f, -1), inverseView),
                new Vector3(2.6055727f, 2.18612f, 7.069948f));
        }
    }

    internal static ProjectionResult Project(Vector3 world)
    {
        Matrix4x4 worldToClip = View * Projection;
        Vector4 clip = Vector4.Transform(new Vector4(world, 1), worldToClip);
        bool inside = clip.W > 0 &&
            clip.X >= -clip.W && clip.X <= clip.W &&
            clip.Y >= -clip.W && clip.Y <= clip.W &&
            clip.Z >= 0 && clip.Z <= clip.W;
        Vector3 ndc = new(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
        return new ProjectionResult(
            new Vector2(
                (ndc.X + 1) * RasterWidth * 0.5f,
                (1 - ndc.Y) * RasterHeight * 0.5f),
            ndc.Z,
            inside);
    }

    internal static bool ClipToNearPlane(
        Vector3 start,
        Vector3 end,
        out Vector3 clippedStart,
        out Vector3 clippedEnd)
    {
        Vector3 viewStart = Vector3.Transform(start, View);
        Vector3 viewEnd = Vector3.Transform(end, View);
        bool startInside = viewStart.Z <= -NearPlane;
        bool endInside = viewEnd.Z <= -NearPlane;
        clippedStart = start;
        clippedEnd = end;
        if (!startInside && !endInside) { return false; }
        if (startInside && endInside) { return true; }
        float amount = (-NearPlane - viewStart.Z) / (viewEnd.Z - viewStart.Z);
        Vector3 intersection = Vector3.Lerp(start, end, amount);
        if (!startInside) { clippedStart = intersection; }
        else { clippedEnd = intersection; }
        return true;
    }

    internal static PrimitivePixelMask CreateButtLineMask(
        int width,
        int height,
        Vector2 start,
        Vector2 end,
        float thickness)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (!(length > 0) || !float.IsFinite(length) ||
            !(thickness > 0) || !float.IsFinite(thickness))
        {
            throw new ArgumentOutOfRangeException(nameof(thickness));
        }

        Vector2 axis = delta / length;
        Vector2 normal = new(-axis.Y, axis.X);
        float halfThickness = thickness * 0.5f;
        Vector2 center = (start + end) * 0.5f;
        float extentX = MathF.Abs(normal.X) * halfThickness;
        float extentY = MathF.Abs(normal.Y) * halfThickness;
        OracleBounds bounds = new(
            MathF.Min(start.X, end.X) - extentX,
            MathF.Min(start.Y, end.Y) - extentY,
            MathF.Max(start.X, end.X) + extentX,
            MathF.Max(start.Y, end.Y) + extentY);
        return CreateMask(width, height, bounds, pixelCenter =>
        {
            Vector2 local = pixelCenter - center;
            Vector2 fromEdge = new(
                MathF.Abs(Vector2.Dot(local, axis)) - (length * 0.5f),
                MathF.Abs(Vector2.Dot(local, normal)) - halfThickness);
            Vector2 outside = Vector2.Max(fromEdge, Vector2.Zero);
            return outside.Length() + MathF.Min(MathF.Max(fromEdge.X, fromEdge.Y), 0);
        });
    }

    internal static PrimitivePixelMask CreateDiscMask(
        int width,
        int height,
        Vector2 center,
        float diameter)
    {
        if (!(diameter > 0) || !float.IsFinite(diameter))
        {
            throw new ArgumentOutOfRangeException(nameof(diameter));
        }

        float radius = diameter * 0.5f;
        OracleBounds bounds = new(
            center.X - radius,
            center.Y - radius,
            center.X + radius,
            center.Y + radius);
        return CreateMask(width, height, bounds, pixelCenter =>
            Vector2.Distance(pixelCenter, center) - radius);
    }

    internal static MaskCoverageResult EvaluateCoverage(
        PrimitivePixelMask mask,
        ReadOnlySpan<Color> actual,
        Color primitiveColor,
        Color clearColor,
        MaskCoverageExpectations expectations)
    {
        if (actual.Length != mask.Samples.Length)
        {
            throw new ArgumentException("The pixel buffer must match the frozen oracle mask.", nameof(actual));
        }

        int interiorViolations = 0;
        int boundaryCovered = 0;
        int boundaryPartial = 0;
        int exteriorHalo = 0;
        float minCoveredX = float.PositiveInfinity;
        float minCoveredY = float.PositiveInfinity;
        float maxCoveredX = float.NegativeInfinity;
        float maxCoveredY = float.NegativeInfinity;
        for (int index = 0; index < actual.Length; index++)
        {
            PixelMaskSample sample = mask.Samples[index];
            int primitiveDelta = MaximumChannelDelta(actual[index], primitiveColor);
            int clearDelta = MaximumChannelDelta(actual[index], clearColor);
            switch (sample.Classification)
            {
                case PixelMaskClassification.OpaqueInterior:
                    if (primitiveDelta > OpaqueInteriorChannelTolerance) { interiorViolations++; }
                    break;
                case PixelMaskClassification.BoundaryBand:
                    if (clearDelta > OpaqueInteriorChannelTolerance)
                    {
                        boundaryCovered++;
                        if (primitiveDelta > OpaqueInteriorChannelTolerance) { boundaryPartial++; }
                    }
                    break;
                case PixelMaskClassification.Exterior:
                    if (clearDelta > OpaqueInteriorChannelTolerance) { exteriorHalo++; }
                    break;
            }

            if (clearDelta > OpaqueInteriorChannelTolerance)
            {
                int x = index % mask.Width;
                int y = index / mask.Width;
                float centerX = x + 0.5f;
                float centerY = y + 0.5f;
                minCoveredX = MathF.Min(minCoveredX, centerX);
                minCoveredY = MathF.Min(minCoveredY, centerY);
                maxCoveredX = MathF.Max(maxCoveredX, centerX);
                maxCoveredY = MathF.Max(maxCoveredY, centerY);
            }
        }

        bool hasCoverage = float.IsFinite(minCoveredX);
        bool reachesAnalyticExtents = hasCoverage &&
            minCoveredX <= mask.AnalyticBounds.Left + ProjectionPixelTolerance &&
            minCoveredY <= mask.AnalyticBounds.Top + ProjectionPixelTolerance &&
            maxCoveredX >= mask.AnalyticBounds.Right - ProjectionPixelTolerance &&
            maxCoveredY >= mask.AnalyticBounds.Bottom - ProjectionPixelTolerance;
        bool staysInsideBoundaryBand = hasCoverage &&
            minCoveredX >= mask.AnalyticBounds.Left - AntialiasingBandPixels &&
            minCoveredY >= mask.AnalyticBounds.Top - AntialiasingBandPixels &&
            maxCoveredX <= mask.AnalyticBounds.Right + AntialiasingBandPixels &&
            maxCoveredY <= mask.AnalyticBounds.Bottom + AntialiasingBandPixels;
        return new MaskCoverageResult(
            interiorViolations,
            boundaryCovered,
            boundaryPartial,
            exteriorHalo,
            reachesAnalyticExtents,
            staysInsideBoundaryBand,
            expectations);
    }

    private static PrimitivePixelMask CreateMask(
        int width,
        int height,
        OracleBounds bounds,
        Func<Vector2, float> signedDistance)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        PixelMaskSample[] samples = new PixelMaskSample[checked(width * height)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = signedDistance(new Vector2(x + 0.5f, y + 0.5f));
                PixelMaskClassification classification = distance < -AntialiasingBandPixels
                    ? PixelMaskClassification.OpaqueInterior
                    : distance <= AntialiasingBandPixels
                        ? PixelMaskClassification.BoundaryBand
                        : PixelMaskClassification.Exterior;
                samples[(y * width) + x] = new PixelMaskSample(classification, distance);
            }
        }
        return new PrimitivePixelMask(width, height, bounds, samples);
    }

    private static int MaximumChannelDelta(Color first, Color second) =>
        Math.Max(
            Math.Max(Math.Abs(first.R - second.R), Math.Abs(first.G - second.G)),
            Math.Max(Math.Abs(first.B - second.B), Math.Abs(first.A - second.A)));

    internal readonly record struct ProjectionCase(
        Vector3 World,
        Vector2 ExpectedPixel,
        float ExpectedDepth);

    internal readonly record struct ProjectionResult(
        Vector2 Pixel,
        float Depth,
        bool IsInsideClipVolume);

    internal readonly record struct CrossingLinesScenario(
        Vector3 HorizontalStart,
        Vector3 HorizontalEnd,
        Vector3 VerticalStart,
        Vector3 VerticalEnd,
        Vector2 ExpectedHorizontalStartPixel,
        Vector2 ExpectedHorizontalEndPixel,
        Vector2 ExpectedVerticalStartPixel,
        Vector2 ExpectedVerticalEndPixel,
        float ExpectedHorizontalDepth,
        float ExpectedVerticalDepth);

    internal readonly record struct NearPlaneScenario(
        Vector3 Start,
        Vector3 End,
        Vector3 ExpectedClippedStart);

    internal readonly record struct OracleBounds(
        float Left,
        float Top,
        float Right,
        float Bottom);

    internal readonly record struct PixelMaskSample(
        PixelMaskClassification Classification,
        float SignedDistance);

    internal sealed record PrimitivePixelMask(
        int Width,
        int Height,
        OracleBounds AnalyticBounds,
        PixelMaskSample[] Samples)
    {
        internal PixelMaskSample this[int x, int y] => Samples[(y * Width) + x];
    }

    internal readonly record struct MaskCoverageExpectations(
        bool RequirePartialBoundaryCoverage,
        bool RequireFullAnalyticExtents)
    {
        internal static MaskCoverageExpectations FractionalPhaseUnoccluded => new(true, true);

        internal static MaskCoverageExpectations PixelAlignedUnoccluded => new(false, true);
    }

    internal readonly record struct MaskCoverageResult(
        int OpaqueInteriorViolationCount,
        int BoundaryCoveredSampleCount,
        int BoundaryPartialSampleCount,
        int ExteriorHaloSampleCount,
        bool ReachesAnalyticExtents,
        bool StaysInsideBoundaryBand,
        MaskCoverageExpectations Expectations)
    {
        internal bool Passes =>
            OpaqueInteriorViolationCount == 0 &&
            BoundaryCoveredSampleCount > 0 &&
            (!Expectations.RequirePartialBoundaryCoverage || BoundaryPartialSampleCount > 0) &&
            ExteriorHaloSampleCount == 0 &&
            (!Expectations.RequireFullAnalyticExtents || ReachesAnalyticExtents) &&
            StaysInsideBoundaryBand;
    }
}

internal enum PixelMaskClassification
{
    OpaqueInterior,
    BoundaryBand,
    Exterior
}
