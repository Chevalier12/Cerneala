using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismCustomConvolutionGpuTests
{
    [SdlNativeFact]
    public void GpuMatchesCpuForNegativeUnnormalizedKernelAndWrapEdges()
    {
        const int width = 3;
        using SdlPrismKernelFixture fixture = new();
        PrismCatalogFilterPlan plan = CreatePlan();
        Assert.Single(plan.Passes);
        Vector4[] source = SourcePixels().Select(color => new Vector4(
            (float)color.Red, (float)color.Green, (float)color.Blue, (float)color.Alpha)).ToArray();
        Vector4[] weights = new Vector4[9];
        weights[3] = new Vector4(-1, 0, 0, 0);
        weights[4] = new Vector4(3, 0, 0, 0);
        Vector4[] gpuPixels = fixture.RunCatalog(plan, source, width, 1,
            secondary: new SdlPrismTextureData(3, 3, weights), sampling: DrawSamplingMode.Point);
        PrismPremultipliedColor[] cpuPixels =
            PrismCatalogFilterMath.Apply(
                plan,
                SourcePixels(),
                width,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource: DifferenceKernel);

        Assert.Equal(9, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.CustomConvolution));
        for (int index = 0; index < width; index++)
        {
            Vector4 gpu = gpuPixels[index];
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.003f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.003f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.003f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.003f);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.CustomConvolution,
            [
                new(
                    0,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: false),
                new(
                    1,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "EdgeMode",
                        "Wrap")),
                new(
                    2,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: new PrismResourceId("custom-kernel")),
                Number(3, 0.01f),
                Number(4, 0.5f)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 3, 1));

    private static PrismGraphParameter Number(
        int slot,
        float value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Number,
            numberValue: value);

    private static PrismPremultipliedColor[] SourcePixels() =>
    [
        PrismPremultipliedColor.FromStraight(0.05, 0.02, 0.01, 0.5),
        PrismPremultipliedColor.FromStraight(0.1, 0.04, 0.02, 0.5),
        PrismPremultipliedColor.FromStraight(0.15, 0.06, 0.03, 0.5)
    ];

    private static System.Numerics.Vector4 DifferenceKernel(
        System.Numerics.Vector2 uv)
    {
        int x = Math.Clamp((int)(uv.X * 3), 0, 2);
        int y = Math.Clamp((int)(uv.Y * 3), 0, 2);
        float weight = (x, y) switch
        {
            (0, 1) => -1,
            (1, 1) => 3,
            _ => 0
        };
        return new System.Numerics.Vector4(weight, 0, 0, 0);
    }
}
