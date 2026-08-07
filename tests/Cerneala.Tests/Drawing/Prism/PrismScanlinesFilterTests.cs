using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismScanlinesFilterTests
{
    private const int Width = 3;
    private const int Height = 24;

    [Fact]
    public void SoftnessControlsAContinuousGeneralizedGaussianProfile()
    {
        PrismCatalogFilterPlan hardPlan =
            CreatePlan(1, 0.5f, 0, 1, 0);
        PrismCatalogFilterPlan softPlan =
            CreatePlan(1, 0.5f, 0, 1, 1);
        Assert.Equal(0, PrismCatalogFilterMath.Option(hardPlan, "Softness", -1));
        Assert.Equal(1, PrismCatalogFilterMath.Option(softPlan, "Softness", -1));
        PrismPremultipliedColor[] hard = Apply(hardPlan);
        PrismPremultipliedColor[] soft = Apply(softPlan);

        double[] hardCoverage = Coverage(hard);
        double[] softCoverage = Coverage(soft);
        int center = Array.IndexOf(hardCoverage, hardCoverage.Max());
        int edge = center - (Height / 4);

        Assert.True(hardCoverage[center] > 0.95);
        Assert.True(softCoverage[center] > 0.9);
        Assert.True(
            hardCoverage[edge] < 0.05,
            $"Hard profile: {string.Join(", ", hardCoverage.Select(value => value.ToString("F4")))}");
        Assert.True(
            softCoverage.Sum() > hardCoverage.Sum(),
            $"Hard={hardCoverage.Sum():F4}; Soft={softCoverage.Sum():F4}; " +
            $"Soft profile: {string.Join(", ", softCoverage.Select(value => value.ToString("F4")))}");
        Assert.True(softCoverage.DistinctBy(value => Math.Round(value, 4)).Count() > 4);
    }

    [Fact]
    public void FractionalFrequencyUsesSubpixelCoverageInsteadOfBinaryBands()
    {
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(7.5f, 0.4f, 0.17f, 1, 0.35f));
        double[] coverage = Coverage(result);

        Assert.Contains(coverage, value => value is > 0.05 and < 0.95);
        Assert.True(
            coverage.DistinctBy(value => Math.Round(value, 4)).Count() > 5,
            $"Coverage: {string.Join(", ", coverage.Select(value => value.ToString("F4")))}");
    }

    [Fact]
    public void PhaseMovesTheProfileWithoutChangingItsEnergy()
    {
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(2, 0.45f, 0, 1, 0.4f));
        PrismPremultipliedColor[] shifted = Apply(
            CreatePlan(2, 0.45f, 0.25f, 1, 0.4f));
        double[] baselineCoverage = Coverage(baseline);
        double[] shiftedCoverage = Coverage(shifted);

        Assert.False(baselineCoverage.SequenceEqual(shiftedCoverage));
        Assert.InRange(
            shiftedCoverage.Sum() / baselineCoverage.Sum(),
            0.95,
            1.05);
    }

    [Fact]
    public void LineColorPreservesAssociatedAlpha()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(0.8, 0.4, 0.2, 0.35);
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(
                1,
                0.8f,
                0,
                1,
                0.5f,
                new Color(128, 32, 224, 96)),
            source);

        foreach (PrismPremultipliedColor pixel in result)
        {
            Assert.Equal(source.Alpha, pixel.Alpha, 6);
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
        }
    }

    [Fact]
    public void ZeroLineOpacityIsIdentity()
    {
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(0.25, 0.5, 0.75, 0.4);
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(11.5f, 0.75f, -0.3f, 0, 1),
            source);

        foreach (PrismPremultipliedColor pixel in result)
        {
            Assert.Equal(source.Red, pixel.Red, 6);
            Assert.Equal(source.Green, pixel.Green, 6);
            Assert.Equal(source.Blue, pixel.Blue, 6);
            Assert.Equal(source.Alpha, pixel.Alpha, 6);
        }
    }

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor? fill = null)
    {
        PrismPremultipliedColor source = fill ??
            PrismPremultipliedColor.FromStraight(1, 1, 1, 1);
        return PrismCatalogFilterMath.Apply(
            plan,
            Enumerable.Repeat(source, Width * Height).ToArray(),
            Width,
            Height,
            PrismColorProfile.LinearSrgb);
    }

    private static double[] Coverage(PrismPremultipliedColor[] pixels) =>
        Enumerable.Range(0, Height)
            .Select(y => 1 - pixels[(y * Width) + (Width / 2)].Red)
            .ToArray();

    private static PrismCatalogFilterPlan CreatePlan(
        float frequency,
        float thickness,
        float phase,
        float lineOpacity,
        float softness,
        Color? color = null)
    {
        ImmutableArray<PrismGraphParameter> parameters =
        [
            ColorParameter(0, color ?? new Color(0, 0, 0, 255)),
            Number(1, frequency),
            Number(2, lineOpacity),
            Number(3, phase),
            Number(4, softness),
            Number(5, thickness)
        ];

        return PrismCatalogFilterPlanner.Create(
            PrismFilterId.Scanlines,
            parameters,
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, Width, Height));
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);
}
