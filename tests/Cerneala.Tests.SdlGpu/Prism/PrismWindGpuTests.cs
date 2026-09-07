using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;
using Cerneala.Tests.SdlGpu;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismWindGpuTests
{
    [Fact]
    public void SelectorRoutesWindToDedicatedKernel()
    {
        Assert.Equal(14, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.Wind));
    }

    [SdlNativeFact]
    public void ShaderPackageCreatesFloatingPointPipeline()
    {
        using SdlPrismKernelFixture fixture = new();
        Assert.NotEqual((nint)0, fixture.FloatPipeline);
    }
    [SdlNativeFact]
    public void LicPipelineChangesImageAndHonorsDirection()
    {
        const int width = 41;
        const int height = 23;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan rightPlan = CreatePlan("FromRight", width, height);
        PrismCatalogFilterPlan leftPlan = CreatePlan("FromLeft", width, height);
        Vector4[] rightPixels = fixture.RunCatalog(rightPlan, sourcePixels, width, height);
        Vector4[] leftPixels = fixture.RunCatalog(leftPlan, sourcePixels, width, height);
        double sourceDifference = 0;
        double directionDifference = 0;
        for (int index = 0; index < rightPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index];
            Vector4 right = rightPixels[index];
            Vector4 left = leftPixels[index];
            sourceDifference += ColorDifference(source, right);
            directionDifference += ColorDifference(right, left);
            Assert.InRange(Math.Abs(source.W - right.W), 0, 0.015f);
            AssertAssociated(right);
            AssertAssociated(left);
        }

        double meanSourceDifference = sourceDifference / rightPixels.Length;
        double meanDirectionDifference =
            directionDifference / rightPixels.Length;
        Assert.True(
            meanSourceDifference > 0.015,
            $"Mean source difference was {meanSourceDifference}.");
        Assert.True(
            meanDirectionDifference > 0.0001,
            $"Mean direction difference was {meanDirectionDifference}.");
    }

    private static PrismCatalogFilterPlan CreatePlan(
        string direction,
        int width,
        int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Wind,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "Direction",
                        direction)),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "Method",
                        "Wind")),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: 17),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 4)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        int center = width / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.55f + (0.4f * x / (width - 1f));
                float noise = ((x * 7 + y * 13) & 7) / 100f;
                float red = x == center
                    ? 0.92f
                    : x == center + 1
                        ? 0.48f
                        : 0.03f + (0.18f * x / (width - 1f));
                float green = x < center
                    ? 0.08f + noise
                    : 0.3f + noise;
                float blue = x == center ? 0.12f : 0.04f + noise;
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    red * alpha,
                    green * alpha,
                    blue * alpha,
                    alpha));
            }
        }
        return pixels;
    }

    private static double ColorDifference(Vector4 left, Vector4 right) =>
        Math.Abs(left.X - right.X) +
        Math.Abs(left.Y - right.Y) +
        Math.Abs(left.Z - right.Z);

    private static void AssertAssociated(Vector4 color)
    {
        Assert.True(float.IsFinite(color.X));
        Assert.True(float.IsFinite(color.Y));
        Assert.True(float.IsFinite(color.Z));
        Assert.True(float.IsFinite(color.W));
        Assert.InRange(color.W, 0, 1.001f);
        Assert.InRange(color.X, 0, color.W + 0.015f);
        Assert.InRange(color.Y, 0, color.W + 0.015f);
        Assert.InRange(color.Z, 0, color.W + 0.015f);
    }
}
