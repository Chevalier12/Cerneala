using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismInkOutlinesFilterTests
{
    [Fact]
    public void PlannerBuildsScaledDualGaussianAndCompositePasses()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            strokeLength: 4,
            darkIntensity: 20,
            lightIntensity: 10,
            pixelScale: 2,
            effectiveTransform: Matrix3x2.CreateScale(1.5f));

        Assert.Equal(3, plan.Passes.Length);
        Assert.Equal(
            [
                PrismCatalogFilterPassKind.Horizontal,
                PrismCatalogFilterPassKind.Vertical,
                PrismCatalogFilterPassKind.Direct
            ],
            plan.Passes.Select(pass => pass.Kind));
        Assert.Equal([0, 1, 2], plan.Passes.Select(pass => pass.Iteration));
        Assert.Equal(4, plan.GetOption("StrokeLength").X);
        Assert.Equal(20, plan.GetOption("DarkIntensity").X);
        Assert.Equal(10, plan.GetOption("LightIntensity").X);
        Assert.Equal(3, plan.Options3.X, 5);
        Assert.Equal(4.8f, plan.Options3.Y, 5);
        Assert.Equal(6, plan.Options3.Z);
        Assert.Equal(8, plan.Passes[0].RadiusX);
        Assert.Equal(8, plan.Passes[1].RadiusY);
        Assert.False(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[1]));
        Assert.True(PrismCatalogFilterPlanner.RequiresOriginalInput(
            plan.Filter,
            plan.Passes[2]));
    }

    [Fact]
    public void CpuReferenceIsDeterministicAndEachControlChangesTheResult()
    {
        const int width = 31;
        const int height = 19;
        PrismPremultipliedColor[] source = CreateEdge(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(4, 20, 10), source, width, height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(4, 20, 10), source, width, height);
        PrismPremultipliedColor[] wider = Apply(
            CreatePlan(8, 20, 10), source, width, height);
        PrismPremultipliedColor[] darker = Apply(
            CreatePlan(4, 50, 10), source, width, height);
        PrismPremultipliedColor[] lighter = Apply(
            CreatePlan(4, 20, 40), source, width, height);

        Assert.Equal(baseline, repeated);
        double baselineDifference = MeanDifference(source, baseline);
        double widthDifference = MeanDifference(baseline, wider);
        double darkDifference = MeanDifference(baseline, darker);
        double lightDifference = MeanDifference(baseline, lighter);
        Assert.True(
            baselineDifference > 0.001,
            $"baseline difference: {baselineDifference}");
        Assert.True(
            widthDifference > 0.0001,
            $"width difference: {widthDifference}");
        Assert.True(
            darkDifference > 0.0001,
            $"dark difference: {darkDifference}");
        Assert.True(
            lightDifference > 0.0001,
            $"light difference: {lightDifference}");
        Assert.True(MeanLuminance(lighter) > MeanLuminance(baseline));

        for (int index = 0; index < baseline.Length; index++)
        {
            PrismPremultipliedColor pixel = baseline[index];
            Assert.Equal(source[index].Alpha, pixel.Alpha, 5);
            Assert.True(double.IsFinite(pixel.Red));
            Assert.InRange(pixel.Red, 0, pixel.Alpha);
            Assert.InRange(pixel.Green, 0, pixel.Alpha);
            Assert.InRange(pixel.Blue, 0, pixel.Alpha);
        }
    }

    [Fact]
    public void ZeroDarkAndLightIntensityPreserveTheSource()
    {
        const int width = 17;
        const int height = 11;
        PrismPremultipliedColor[] source =
            CreateGrayscaleEdge(width, height);

        PrismPremultipliedColor[] result = Apply(
            CreatePlan(4, 0, 0), source, width, height);

        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Red, result[index].Red, 5);
            Assert.Equal(source[index].Green, result[index].Green, 5);
            Assert.Equal(source[index].Blue, result[index].Blue, 5);
            Assert.Equal(source[index].Alpha, result[index].Alpha, 5);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float strokeLength,
        float darkIntensity,
        float lightIntensity,
        float pixelScale = 1,
        Matrix3x2? effectiveTransform = null) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.InkOutlines,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: darkIntensity),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: lightIntensity),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: strokeLength)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            effectiveTransform ?? Matrix3x2.Identity,
            new DrawRect(0, 0, 31, 19));

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.Apply(
            plan,
            source,
            width,
            height,
            PrismColorProfile.LinearSrgb);

    private static PrismPremultipliedColor[] CreateEdge(int width, int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double red = x < width / 2 ? 0.12 : 0.82;
                double green = x < width / 2 ? 0.22 : 0.68;
                double blue = x < width / 2 ? 0.34 : 0.38;
                double alpha = (x + y) % 7 == 0 ? 0.45 : 0.8;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        red,
                        green,
                        blue,
                        alpha);
            }
        }

        return pixels;
    }

    private static PrismPremultipliedColor[] CreateGrayscaleEdge(
        int width,
        int height)
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = x < width / 2 ? 0.2 : 0.8;
                double alpha = (x + y) % 7 == 0 ? 0.45 : 0.8;
                pixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value,
                        value,
                        alpha);
            }
        }

        return pixels;
    }

    private static double MeanDifference(
        PrismPremultipliedColor[] first,
        PrismPremultipliedColor[] second) =>
        first.Zip(second, (left, right) =>
                Math.Abs(left.Red - right.Red) +
                Math.Abs(left.Green - right.Green) +
                Math.Abs(left.Blue - right.Blue))
            .Average();

    private static double MeanLuminance(
        PrismPremultipliedColor[] pixels) =>
        pixels.Average(pixel => pixel.Alpha <= 0
            ? 0
            : (((pixel.Red / pixel.Alpha) * 0.2126) +
                ((pixel.Green / pixel.Alpha) * 0.7152) +
                ((pixel.Blue / pixel.Alpha) * 0.0722)));
}
