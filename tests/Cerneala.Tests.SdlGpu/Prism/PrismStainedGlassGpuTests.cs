using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismStainedGlassGpuTests
{
    [Fact]
    public void SelectorRoutesStainedGlassToDedicatedShaderKernel() =>
        Assert.Equal(28, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(
            PrismFilterId.StainedGlass, 0));

    [SdlNativeFact]
    public void JumpFloodPipelineMatchesCpuFallback()
    {
        const int width = 32;
        const int height = 24;
        using SdlPrismKernelFixture fixture = new();
        PrismPremultipliedColor[] sourceColors =
            PrismStainedGlassTestData.CreateSubject(width, height);
        PrismCatalogFilterPlan plan = PrismStainedGlassTestData.CreatePlan(
            8, 0, 0, Color.Black, 1937, width, height);
        Vector4[] gpuPixels = fixture.RunCatalog(plan,
            sourceColors.Select(color => new Vector4((float)color.Red, (float)color.Green,
                (float)color.Blue, (float)color.Alpha)).ToArray(),
            width, height, sampling: DrawSamplingMode.Point);
        PrismPremultipliedColor[] cpuPixels = PrismCatalogFilterMath.Apply(
            plan, sourceColors, width, height, PrismColorProfile.LinearSrgb);

        int boundaryShiftPixels = 0;
        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index];
            PrismPremultipliedColor cpu = cpuPixels[index];
            float samePixelRgbError = Math.Max(
                Math.Abs(gpu.X - (float)cpu.Red),
                Math.Max(Math.Abs(gpu.Y - (float)cpu.Green), Math.Abs(gpu.Z - (float)cpu.Blue)));
            if (samePixelRgbError > 0.035f)
            {
                boundaryShiftPixels++;
                Assert.InRange(samePixelRgbError, 0, 0.065f);
            }
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.012f);
        }

        int maximumBoundaryShiftPixels = (gpuPixels.Length + 49) / 50;
        Assert.InRange(boundaryShiftPixels, 0, maximumBoundaryShiftPixels);
    }
}
