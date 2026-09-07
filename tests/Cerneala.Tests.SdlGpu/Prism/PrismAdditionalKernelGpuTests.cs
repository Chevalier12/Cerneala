using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Tests.SdlGpu;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismAdditionalKernelGpuTests
{
    [SdlNativeFact]
    public void ChannelMixerMatchesCpuLinearRgbMatrixAcrossProfiles()
    {
        PrismAdjustmentPlan matrix = new(PrismFilterId.ChannelMixer,
            PrismAdjustmentOperation.ChannelMixer, PrismBlendMode.Normal)
        {
            Parameters0 = new(.5f, .25f, .1f, 0),
            Parameters1 = new(.2f, .6f, .1f, 0),
            Parameters2 = new(.1f, .3f, .5f, 0),
            Parameters3 = new(.05f, .1f, 0, 0)
        };
        PrismPremultipliedColor[] input = [P(.2, .4, .6, .5), P(.7, .1, .3, 1), default];
        using SdlPrismKernelFixture fixture = new();
        foreach (PrismColorProfile profile in Enum.GetValues<PrismColorProfile>())
        foreach (bool monochrome in new[] { false, true })
        {
            PrismAdjustmentPlan plan = matrix with { Parameters4 = new(monochrome ? 1 : 0, 0, 0, 0) };
            PrismPremultipliedColor[] working = input.Select(c => PrismColorPipeline.ConvertInputToWorking(c, profile)).ToArray();
            Vector4[] actual = fixture.RunKernel(3, working.Select(V).ToArray(), working.Length, 1, u =>
            {
                u[23] = new((int)PrismAdjustmentOperation.ChannelMixer, (int)profile, 0, 0);
                u[24] = plan.Parameters0; u[25] = plan.Parameters1; u[26] = plan.Parameters2;
                u[27] = plan.Parameters3; u[28] = plan.Parameters4;
            });
            for (int i = 0; i < actual.Length; i++)
                Near(actual[i], PrismAdjustmentMath.Apply(plan, working[i], profile), .003,
                    $"{profile} monochrome={monochrome} sample {i}");
        }
    }

    [SdlNativeFact]
    public void PosterizeMatchesCpuUniformLinearRgbQuantizationAcrossProfiles()
    {
        PrismAdjustmentPlan plan = new(PrismFilterId.Posterize,
            PrismAdjustmentOperation.Posterize, PrismBlendMode.Normal) { Parameters0 = new(5, 0, 0, 0) };
        PrismPremultipliedColor[] input = [P(0, .376, 1, .4), P(.12, .13, .62, 1), default];
        using SdlPrismKernelFixture fixture = new();
        foreach (PrismColorProfile profile in Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor[] working = input.Select(c => PrismColorPipeline.ConvertInputToWorking(c, profile)).ToArray();
            Vector4[] actual = fixture.RunKernel(3, working.Select(V).ToArray(), working.Length, 1, u =>
            {
                u[0] = new(.5f, 1f / working.Length, 1, 0);
                u[23] = new((int)PrismAdjustmentOperation.Posterize, (int)profile, 0, 0);
                u[24] = plan.Parameters0;
            });
            for (int i = 0; i < actual.Length; i++)
                Near(actual[i], PrismAdjustmentMath.Apply(plan, working[i], profile, opacity: .5f),
                    .003, $"{profile} sample {i}");
        }
    }

    [SdlNativeFact]
    public void TransformMapsTranslationAndTransparentEdges()
    {
        Vector4 red = new(1, 0, 0, 1), green = new(0, .5f, 0, .5f), blue = new(0, 0, 1, 1);
        using SdlPrismKernelFixture fixture = new();
        Vector4[] actual = fixture.RunKernel(8, [red, green, blue], 3, 1, u =>
        {
            u[23] = new((int)PrismResamplingOperation.Transform, (int)PrismColorProfile.LinearSrgb,
                (int)PrismResamplingPassKind.Direct, 0);
            u[24] = new(1, 0, 1, 1);
            u[25] = Vector4.Zero;
            u[26] = new(.5f, .5f, 1, 0);
            u[27] = new(3, 1, 0, 0);
            u[33] = Vector4.Zero;
        });
        Near(actual[0], default, .003, "translated transparent edge");
        Near(actual[1], C(red), .003, "translated red");
        Near(actual[2], C(green), .003, "translated green");
    }

    [SdlNativeFact]
    public void SpherizeMatchesCpuOrthographicProjection()
    {
        const int width = 17, height = 9;
        PrismPremultipliedColor[] source = Enumerable.Range(0, width * height).Select(i =>
            P((i % width) / (double)(width - 1), (i / width) / (double)(height - 1),
                ((i % width) + (i / width)) / (double)(width + height - 2), 1)).ToArray();
        using SdlPrismKernelFixture fixture = new();
        foreach (float amount in new[] { -1f, 1f })
        for (int mode = 0; mode <= 2; mode++)
        {
            PrismResamplingPlan plan = new(PrismFilterId.Spherize, PrismResamplingOperation.Spherize,
                PrismBlendMode.Normal, [new PrismResamplingPass(PrismResamplingPassKind.Direct, IsNoOp: false)])
                { Options0 = new(amount, mode, .4f, .6f) };
            PrismPremultipliedColor[] expected = PrismResamplingMath.Apply(plan, source, width, height, PrismColorProfile.LinearSrgb);
            Vector4[] actual = fixture.RunKernel(8, source.Select(V).ToArray(), width, height, u =>
            {
                u[23] = new((int)PrismResamplingOperation.Spherize, (int)PrismColorProfile.LinearSrgb,
                    (int)PrismResamplingPassKind.Direct, 0);
                u[24] = plan.Options0;
                u[33] = Vector4.Zero;
            });
            for (int i = 0; i < actual.Length; i++)
                Near(actual[i], expected[i], .004, $"amount={amount} mode={mode} sample={i}");
        }
    }

    [SdlNativeFact]
    public void NeighborhoodNoiseIsDeterministicAndUsesPreparedSeed()
    {
        const int width = 256;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] source = Enumerable.Repeat(new Vector4(.5f, .5f, .5f, 1), width).ToArray();
        Vector4[] first = Noise(41), repeated = Noise(41), changed = Noise(42), highSeed = Noise(41 + 65536), gaussian = Noise(41, true);
        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
        Assert.False(first.SequenceEqual(highSeed));
        Assert.Contains(gaussian, pixel => MathF.Abs(pixel.X - .5f) > .4f);
        Assert.DoesNotContain(first, pixel => MathF.Abs(pixel.X - .5f) > .201f);
        Assert.All(first, pixel =>
        {
            Assert.InRange(MathF.Abs(pixel.X - pixel.Y), 0, .001f);
            Assert.InRange(MathF.Abs(pixel.X - pixel.Z), 0, .001f);
        });
        Vector4[] Noise(int seed, bool isGaussian = false) => fixture.RunKernel(7, source, width, 1, u =>
        {
            u[23] = new((int)PrismNeighborhoodOperation.AddNoise, (int)PrismColorProfile.LinearSrgb,
                (int)PrismNeighborhoodPassKind.Direct, 0);
            u[24] = new(.2f, isGaussian ? 1 : 0, 1, seed & 0xffff);
            u[25] = new((seed >> 16) & 0xffff, 0, 0, 0);
            u[33] = new(0, 0, 9, 0);
        });
    }

    [SdlNativeFact]
    public void HalftonePatternPreservesDotAreaColorsAndAlpha()
    {
        const int size = 64;
        const float alpha = .4f;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] pixels = fixture.RunKernel(9,
            Enumerable.Repeat(new Vector4(.7f * alpha, .7f * alpha, .7f * alpha, alpha), size * size).ToArray(),
            size, size, u =>
            {
                u[23] = new((int)PrismFilterId.HalftonePattern, (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Procedural, 0);
                u[24] = new(0, 0, 1, 1); u[25] = Vector4.Zero;
                u[26] = new(1, 0, 0, 1); u[27] = Vector4.Zero;
                u[28] = new(8, 0, 0, 0); u[33] = Vector4.Zero;
            });
        Assert.InRange(pixels.Average(v => v.X / v.W), .25, .35);
        Assert.True(pixels.Max(v => v.X) - pixels.Min(v => v.X) > .2f);
        Assert.All(pixels, v =>
        {
            Assert.InRange(v.W, alpha - .001f, alpha + .001f);
            Assert.InRange(MathF.Abs(v.X + v.Z - v.W), 0, .002f);
        });
    }

    [SdlNativeFact]
    public void WaveNoiseMatchesCpuSpectralEvaluation()
    {
        const int size = 8;
        const float scale = 1.75f;
        const uint seed = 2_000_000_007u;
        PrismWaveNoiseTable table = PrismWaveNoise.Precompute(unchecked((int)seed), new(.03125f, 1, 0, 0), PrismWaveSpectrum.Brown);
        Vector4 foreground = new(.8f, .1f, .2f, 1), background = new(.1f, .6f, .9f, 1);
        using SdlPrismKernelFixture fixture = new();
        Vector4[] pixels = fixture.RunKernel(34, Enumerable.Repeat(Vector4.One, size * size).ToArray(), size, size, u =>
        {
            u[23] = new((int)PrismFilterId.Clouds, (int)PrismColorProfile.LinearSrgb,
                (int)PrismCatalogFilterPrimitive.Procedural, 0);
            u[24] = foreground; u[25] = background; u[26] = new(scale, 0, 0, 0);
            u[27] = new(seed & 0xffffu, seed >> 16, 0, 0);
            u[28] = new(20, 0, 0, 0); u[29] = new(4, 0, 0, 0);
            u[31] = new(0, 1, 0, 0); u[33] = new(table.Normalization, 0, 0, 0);
        }, inputs: new Dictionary<uint, SdlPrismTextureData>
        {
            [13] = new(PrismWaveNoise.PackedTableSampleCount, 1, table.PackedSamples.ToArray())
        });
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float noise = PrismWaveNoise.Sample(table, new((x + .5f) / scale, (y + .5f) / scale), seed, 20, 4, new(0, 1, 0, 0));
            Vector4 expected = Vector4.Lerp(background, foreground, noise);
            Near(pixels[y * size + x], C(expected), .006, $"Wave noise ({x}, {y})");
            Assert.InRange(pixels[y * size + x].W, .999f, 1);
        }
    }

    [SdlNativeFact]
    public void ColorHalftoneProducesChromaticAngleSensitiveScreens()
    {
        const int size = 17;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] source = Enumerable.Repeat(new Vector4(.15f, .45f, .3f, .75f), size * size).ToArray();
        Vector4[] first = Render(new(108, 162, 90, 45)), changed = Render(new(139, 162, 90, 45));
        Assert.All(first, pixel => Assert.InRange(pixel.W, .749f, .751f));
        Assert.Contains(first, p => MathF.Abs(p.X - p.Y) > .02f || MathF.Abs(p.Y - p.Z) > .02f);
        Assert.Contains(first.Zip(changed), pair => Vector4.Distance(pair.First, pair.Second) > .02f);
        Vector4[] Render(Vector4 angles) => fixture.RunKernel(37, source, size, size, u =>
        {
            Vector4 a = angles * (MathF.PI / 180);
            u[23] = new((int)PrismFilterId.ColorHalftone, (int)PrismColorProfile.LinearSrgb,
                (int)PrismCatalogFilterPrimitive.Quantization, 0);
            u[26] = new(MathF.Cos(a.X), MathF.Cos(a.Y), MathF.Cos(a.Z), MathF.Cos(a.W));
            u[27] = new(MathF.Sin(a.X), MathF.Sin(a.Y), MathF.Sin(a.Z), MathF.Sin(a.W));
            u[33] = new(4, 4, 0, 0);
        });
    }

    [SdlNativeFact]
    public void LightingEffectsUsesPackedLightsHeightNormalsAndExposure()
    {
        const int size = 7;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] source = Enumerable.Repeat(new Vector4(.28f, .12f, .04f, .4f), size * size).ToArray();
        SdlPrismTextureData height = new(size, size, Enumerable.Range(0, size * size).Select(i =>
        {
            float value = (i % size) / (float)(size - 1);
            return new Vector4(value, value, value, 1);
        }).ToArray());
        Vector4[] flat = Render(0, 0), relief = Render(8, 0), exposed = Render(8, 1);
        Assert.Contains(flat.Zip(relief), pair => Vector4.Distance(pair.First, pair.Second) > .01f);
        int center = (size / 2) * size + size / 2;
        Assert.True(exposed[center].X > relief[center].X);
        Assert.All(relief, v =>
        {
            Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z) && float.IsFinite(v.W));
            Assert.InRange(v.W, .399f, .401f);
            Assert.InRange(v.X, 0, v.W); Assert.InRange(v.Y, 0, v.W); Assert.InRange(v.Z, 0, v.W);
        });
        Vector4[] Render(float textureHeight, float exposure) => fixture.RunKernel(39, source, size, size, u =>
        {
            u[23] = new((int)PrismFilterId.LightingEffects, (int)PrismColorProfile.LinearSrgb,
                (int)PrismCatalogFilterPrimitive.Procedural, 2);
            u[25] = new(.05f, 0, 0, 0); u[26] = new(.25f, 0, 0, 0); u[27] = new(.8f, 0, 0, 0);
            u[28] = new(exposure, 0, 0, 0); u[30] = new(textureHeight, 0, 0, 0); u[33] = Vector4.Zero;
            u[22] = new(0, 0, 1, 0);
            u[35] = new(0, 1.5f, 0, 0); u[36] = Vector4.Normalize(new(.6f, -.2f, 1, 0));
            u[37] = new(1, .8f, .6f, 0);
        }, inputs: new Dictionary<uint, SdlPrismTextureData> { [6] = height });
    }

    private static PrismPremultipliedColor P(double r, double g, double b, double a) => PrismPremultipliedColor.FromStraight(r, g, b, a);
    private static Vector4 V(PrismPremultipliedColor c) => new((float)c.Red, (float)c.Green, (float)c.Blue, (float)c.Alpha);
    private static PrismPremultipliedColor C(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    private static void Near(Vector4 actual, PrismPremultipliedColor expected, double tolerance, string context)
    {
        Assert.True(Math.Abs(actual.X - expected.Red) <= tolerance, $"{context}: red {actual.X} != {expected.Red}");
        Assert.True(Math.Abs(actual.Y - expected.Green) <= tolerance, $"{context}: green {actual.Y} != {expected.Green}");
        Assert.True(Math.Abs(actual.Z - expected.Blue) <= tolerance, $"{context}: blue {actual.Z} != {expected.Blue}");
        Assert.True(Math.Abs(actual.W - expected.Alpha) <= tolerance, $"{context}: alpha {actual.W} != {expected.Alpha}");
    }
}
