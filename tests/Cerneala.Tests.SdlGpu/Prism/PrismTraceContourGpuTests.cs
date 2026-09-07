using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismTraceContourGpuTests
{
    [SdlNativeFact]
    public void GpuMatchesCpuForLowerAndUpperLevelSetBoundaries()
    {
        const int width = 5;
        const int height = 3;
        const float alpha = 0.6f;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = new Vector4[width * height];
        PrismPremultipliedColor[] cpuSource =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = x < 2 ? 0.25f : 0.75f;
                int index = (y * width) + x;
                sourcePixels[index] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    value * alpha,
                    value * alpha,
                    value * alpha,
                    alpha));
                cpuSource[index] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value,
                        value,
                        alpha);
            }
        }

        AssertMatches("Lower");
        AssertMatches("Upper");

        void AssertMatches(string edge)
        {
            PrismCatalogFilterPlan plan = CreatePlan(edge);
            Assert.Single(plan.Passes);
            Vector4[] gpuPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
            PrismPremultipliedColor[] expected =
                PrismCatalogFilterMath.Apply(
                    plan,
                    cpuSource,
                    width,
                    height,
                    PrismColorProfile.LinearSrgb);
            for (int index = 0; index < expected.Length; index++)
            {
                Vector4 actual = gpuPixels[index];
                Assert.InRange(
                    Math.Abs(actual.X - expected[index].Red),
                    0,
                    0.002f);
                Assert.InRange(
                    Math.Abs(actual.Y - expected[index].Green),
                    0,
                    0.002f);
                Assert.InRange(
                    Math.Abs(actual.Z - expected[index].Blue),
                    0,
                    0.002f);
                Assert.InRange(
                    Math.Abs(actual.W - expected[index].Alpha),
                    0,
                    0.002f);
            }
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(string edge) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.TraceContour,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol("Edge", edge)),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 0.5f)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 5, 3));
}
