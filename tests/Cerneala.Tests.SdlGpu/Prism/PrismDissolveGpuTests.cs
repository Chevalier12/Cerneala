using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismDissolveGpuTests
{
    [SdlNativeFact]
    public void GpuUsesTheSameRankMapAndSeedOffsetAsCpu()
    {
        int size = PrismDissolveBlend.ThresholdSize;
        const int normalizedSeed = 0x1234;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = Enumerable.Repeat(
            new Vector4(0.375f, 0.25f, 0.125f, 0.5f), size * size).ToArray();
        int kernel = 44 + SdlGpuPrismKernelSelector.ResolveBlendMode(PrismBlendMode.Dissolve);
        Vector4[] gpuPixels = fixture.RunKernel(kernel, sourcePixels, size, size,
            uniforms =>
            {
                uniforms[3] = new Vector4(0, 0, 0, normalizedSeed);
                uniforms[6] = new Vector4(0, 0, 1, 0);
            },
            secondary: new SdlPrismTextureData(size, size, new Vector4[size * size]),
            sampling: DrawSamplingMode.Point);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float expected = PrismDissolveBlend.IsSelected(
                    x,
                    y,
                    normalizedSeed,
                    0.5)
                    ? 1
                    : 0;
                Assert.Equal(
                    expected,
                    gpuPixels[(y * size) + x].W);
            }
        }
    }
}
