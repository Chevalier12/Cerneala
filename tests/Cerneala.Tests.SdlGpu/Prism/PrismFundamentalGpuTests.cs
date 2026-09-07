using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismFundamentalGpuTests
{
    [SdlNativeFact]
    public void BlendMatchesAnalyticReferenceForEveryModeAndAlphaCase()
    {
        PrismPremultipliedColor[] source = [P(.82, .21, .43, 1), default,
            P(.76, .34, .12, .62), P(.87, .16, .38, .43)];
        PrismPremultipliedColor[] backdrop = [P(.27, .71, .54, 1), P(.18, .63, .91, .74),
            default, P(.22, .78, .49, .61)];
        using SdlPrismKernelFixture fixture = new();
        foreach (PrismBlendMode mode in Enum.GetValues<PrismBlendMode>())
        {
            Vector4[] actual = Blend(fixture, mode, source, backdrop, PrismBlendOptions.Default);
            for (int i = 0; i < source.Length; i++)
                Near(actual[i], PrismBlendMath.Composite(mode, source[i], backdrop[i],
                    PrismBlendOptions.Default, pixelX: i, pixelY: 0), .003, $"{mode} sample {i}");
        }
    }

    [SdlNativeFact]
    public void BlendHonorsChannelsBlendIfAndKnockout()
    {
        PrismPremultipliedColor source = P(.82, .31, .30, .65);
        PrismPremultipliedColor backdrop = P(.18, .72, .55, .78);
        (PrismBlendMode Mode, PrismBlendOptions Options)[] cases =
        [
            (PrismBlendMode.Screen, PrismBlendOptions.Default with
                { BlendChannels = PrismBlendChannels.Red | PrismBlendChannels.Alpha }),
            (PrismBlendMode.Multiply, PrismBlendOptions.Default with
                { BlendIfChannel = PrismBlendIfChannel.Blue, ThisLayerRange = new(.2f, .4f, .6f, .8f) }),
            (PrismBlendMode.Overlay, PrismBlendOptions.Default with { Knockout = PrismKnockout.Deep })
        ];
        using SdlPrismKernelFixture fixture = new();
        foreach (var item in cases)
            Near(Assert.Single(Blend(fixture, item.Mode, [source], [backdrop], item.Options)),
                PrismBlendMath.Composite(item.Mode, source, backdrop, item.Options), .003, item.Mode.ToString());
    }

    [SdlNativeFact]
    public void BlendMatchesDualBackdropKnockoutRecurrence()
    {
        PrismPremultipliedColor original = P(.1, .2, .3, .4);
        PrismPremultipliedColor current = new(.42, .09, .16, .7);
        PrismPremultipliedColor source = P(.2, .9, .5, .4);
        using SdlPrismKernelFixture fixture = new();
        Vector4 actual = Assert.Single(Blend(fixture, PrismBlendMode.Multiply, [source], [current],
            PrismBlendOptions.Default with { Knockout = PrismKnockout.Deep },
            new Dictionary<uint, SdlPrismTextureData>
            {
                [2] = new(1, 1, [V(original)]),
                [3] = new(1, 1, [new Vector4(0, 0, 0, .8f)])
            }));
        Near(actual, PrismBlendMath.CompositeKnockout(PrismBlendMode.Multiply, source,
            current, original, sourceShape: .8), .003, "dual-backdrop knockout");
    }

    [SdlNativeFact]
    public void DissolveIsDeterministicAndSeededPerLayer()
    {
        PrismPremultipliedColor source = P(.91, .24, .12, .45);
        PrismPremultipliedColor backdrop = P(.12, .38, .83, 1);
        PrismBlendOptions options = PrismBlendOptions.Default with { DissolveSeed = 17, LayerIdentity = 42 };
        using SdlPrismKernelFixture fixture = new();
        PrismPremultipliedColor[] sources = Enumerable.Repeat(source, 64).ToArray();
        PrismPremultipliedColor[] backdrops = Enumerable.Repeat(backdrop, 64).ToArray();
        Vector4[] first = Blend(fixture, PrismBlendMode.Dissolve, sources, backdrops, options);
        Vector4[] repeated = Blend(fixture, PrismBlendMode.Dissolve, sources, backdrops, options);
        Vector4[] changed = Blend(fixture, PrismBlendMode.Dissolve, sources, backdrops,
            options with { DissolveSeed = 18 });
        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
        for (int i = 0; i < first.Length; i++)
            Near(first[i], PrismBlendMath.Composite(PrismBlendMode.Dissolve, source, backdrop,
                options, pixelX: i, pixelY: 0), .003, $"Dissolve pixel {i}");
    }

    [SdlNativeFact]
    public void ColorRoundTripsEveryProfileWithinTheGoldenTolerance()
    {
        Vector4[] samples = [Vector4.Zero, new(255, 127, 63, 0), new(13, 6, 2, 17),
            new(32, 64, 96, 128), new(250, 125, 5, 255), new(0, 255, 64, 255)];
        samples = samples.Select(v => v / 255f).ToArray();
        using SdlPrismKernelFixture fixture = new();
        foreach (PrismColorProfile profile in Enum.GetValues<PrismColorProfile>())
        {
            Vector4[] working = fixture.RunKernel(SdlGpuPrismKernelSelector.ForInputColorProfile(profile),
                samples, samples.Length, 1);
            Vector4[] actual = fixture.RunKernel(SdlGpuPrismKernelSelector.ForPresentation(profile),
                working, samples.Length, 1, outputFormat: SdlGpuTextureFormat.R8G8B8A8Unorm);
            for (int i = 0; i < samples.Length; i++)
            {
                PrismPremultipliedColor expectedWorking = PrismColorPipeline.ConvertInputToWorking(C(samples[i]), profile);
                Near(working[i], expectedWorking, .001, $"{profile} working sample {i}");
                Near(actual[i], PrismColorPipeline.ConvertWorkingToOutput(expectedWorking, profile),
                    2d / 255, $"{profile} output sample {i}");
            }
            Assert.Equal(Vector4.Zero, actual[1]);
        }
    }

    [SdlNativeFact]
    public void FundamentalKernelsPreservePremultipliedAlpha()
    {
        Vector4 source = new Vector4(64, 32, 16, 128) / 255;
        Vector4 secondary = new Vector4(20, 40, 60, 96) / 255;
        using SdlPrismKernelFixture fixture = new();
        (int Kernel, float Opacity, Vector4 Expected)[] cases =
        [
            (0, .5f, source * .5f),
            (40, 1, source * secondary.W),
            (43, 1, source * secondary.W),
            (44, 1, source + secondary * (1 - source.W))
        ];
        foreach (var item in cases)
        {
            Vector4 actual = Assert.Single(fixture.RunKernel(item.Kernel, [source], 1, 1,
                uniforms => uniforms[0] = new(item.Opacity, 1, 1, 0),
                secondary: new(1, 1, [secondary]), outputFormat: SdlGpuTextureFormat.R8G8B8A8Unorm));
            Near(actual, C(item.Expected), 1d / 255, $"fundamental kernel {item.Kernel}");
        }
    }

    [SdlNativeFact]
    public void MaskHonorsChannelInvertDensityTransformAndFeather()
    {
        using SdlPrismKernelFixture fixture = new();
        Vector4[] constant = Enumerable.Repeat(V(P(.8, .2, .1, .4)), 4).ToArray();
        Assert.InRange(Mask(constant, 4, 41, PrismMaskChannel.Alpha, .5f)[0].W, .697f, .703f);
        Assert.InRange(Mask(constant, 4, 41, PrismMaskChannel.Luminance)[0].W, .317f, .324f);
        Assert.InRange(Mask(constant, 4, 41, PrismMaskChannel.Alpha, invert: true)[0].W, .597f, .603f);
        Vector4[] mapped = Mask([Vector4.Zero, Vector4.Zero, Vector4.One, Vector4.One],
            8, 41, PrismMaskChannel.Alpha, uvX: new(.25f, 0, -.5f));
        Assert.InRange(mapped[0].W, 0, .003f);
        Assert.InRange(mapped[3].W, 0, .003f);
        Assert.InRange(mapped[4].W, .997f, 1);
        Assert.InRange(mapped[5].W, .997f, 1);
        Assert.InRange(mapped[7].W, 0, .003f);
        Vector4[] feathered = Mask(Enumerable.Repeat(new Vector4(.25f), 4).ToArray(),
            4, 42, PrismMaskChannel.Alpha, .5f, feather: new(.5f, 0));
        Assert.InRange(feathered[0].W, .622f, .628f);

        Vector4[] Mask(Vector4[] pixels, int width, int kernel, PrismMaskChannel channel,
            float density = 1, bool invert = false, Vector3? uvX = null, Vector2 feather = default) =>
            fixture.RunKernel(kernel, Enumerable.Repeat(Vector4.One, width).ToArray(), width, 1,
                uniforms =>
                {
                    uniforms[6] = new(1, (float)channel, density, invert ? 1 : 0);
                    uniforms[7] = new(uvX ?? new Vector3(1f / width, 0, 0), 0);
                    uniforms[8] = new(0, 0, .5f, 0);
                    uniforms[9] = new(feather, 0, 0);
                }, inputs: new Dictionary<uint, SdlPrismTextureData> { [0] = new(pixels.Length, 1, pixels) });
    }

    private static Vector4[] Blend(SdlPrismKernelFixture fixture, PrismBlendMode mode,
        PrismPremultipliedColor[] source, PrismPremultipliedColor[] backdrop, PrismBlendOptions options,
        IReadOnlyDictionary<uint, SdlPrismTextureData>? inputs = null) =>
        fixture.RunKernel(44 + SdlGpuPrismKernelSelector.ResolveBlendMode(mode),
            source.Select(V).ToArray(), source.Length, 1, uniforms =>
            {
                uniforms[2] = new(
                    options.BlendChannels.HasFlag(PrismBlendChannels.Red) ? 1 : 0,
                    options.BlendChannels.HasFlag(PrismBlendChannels.Green) ? 1 : 0,
                    options.BlendChannels.HasFlag(PrismBlendChannels.Blue) ? 1 : 0,
                    options.BlendChannels.HasFlag(PrismBlendChannels.Alpha) ? 1 : 0);
                uniforms[3] = new((float)options.Knockout, 1, (float)options.BlendIfChannel,
                    PrismBlendMath.NormalizeDissolveSeed(options.DissolveSeed, options.LayerIdentity));
                uniforms[4] = Range(options.ThisLayerRange);
                uniforms[5] = Range(options.UnderlyingRange);
            }, secondary: new(backdrop.Length, 1, backdrop.Select(V).ToArray()),
            inputs: inputs ?? new Dictionary<uint, SdlPrismTextureData>
            {
                [2] = new(backdrop.Length, 1, backdrop.Select(V).ToArray()),
                [3] = new(source.Length, 1, source.Select(V).ToArray())
            });

    private static Vector4 Range(PrismBlendRange value) => new(value.BlackStart, value.BlackEnd, value.WhiteStart, value.WhiteEnd);
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
