using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using System.Numerics;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismStrokeDistanceFieldGpuTests
{
    [SdlNativeFact]
    public void SeedPassUsesDirectionalCoverageForDiagonalEdge()
    {
        const int size = 5;
        const float strokeShaderKind = 9f;
        const int center = size / 2;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = new Vector4[size * size];
        sourcePixels[(center * size) + center] = Alpha(0.25f);
        sourcePixels[(center * size) + center + 1] = Alpha(1f);
        sourcePixels[((center + 1) * size) + center] = Alpha(1f);
        Vector4[] seeds = fixture.RunKernel(85, sourcePixels, size, size,
            uniforms => uniforms[16] = new Vector4(strokeShaderKind, 0, 0, 0),
            sampling: DrawSamplingMode.Point, outputFormat: SdlGpuTextureFormat.R32G32B32A32Float);
        Vector4 seed = seeds[(center * size) + center];
        float expectedCoordinate =
            ((center + 0.5f) + 0.1464466f) / size;

        Assert.InRange(seed.X, expectedCoordinate - 0.001f, expectedCoordinate + 0.001f);
        Assert.InRange(seed.Y, expectedCoordinate - 0.001f, expectedCoordinate + 0.001f);
        Assert.Equal(1f, seed.W);
    }

    private static Vector4 Alpha(float alpha) => new(0, 0, 0, alpha);
}
