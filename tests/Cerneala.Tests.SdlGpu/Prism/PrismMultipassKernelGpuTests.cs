using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismMultipassKernelGpuTests
{
    [SdlNativeFact]
    public void ColoredPencilRunsTensorBlurAndSwingBilateralComposite()
    {
        const int size = 17;
        const float alpha = .65f;
        Vector4[] source = Pixels(size, size, (x, y) => new Vector4(
            (x + y < size ? new Vector3(.15f, .45f, .75f) : new Vector3(.85f, .35f, .2f)) * alpha, alpha));
        using SdlPrismKernelFixture fixture = new();
        Vector4[] actual = Render(8), soft = Render(2);
        Assert.All(actual, v => Assert.InRange(v.W, alpha - .002f, alpha + .002f));
        Changed(actual, source, .05f);
        Changed(actual, soft, .01f);
        Vector4[] Render(float pressure) => Run(fixture, source, size, size, 17, PrismFilterId.ColoredPencil,
            [(0, 1, 1), (5, 2, 0), (10, 0, 2), (15, 3, 3)], u =>
            {
                u[24] = new(3, 0, 0, 0); u[25] = new(pressure, 0, 0, 0);
                u[26] = new(.25f, 0, 0, 0); u[27] = Vector4.One;
            });
    }

    [SdlNativeFact]
    public void FrescoRunsSmoothedTensorAndAnisotropicKuwaharaComposite()
    {
        const int size = 17;
        const float alpha = .65f;
        Vector4[] source = GrayStep(size, size, alpha, .15f, .85f, .08f);
        using SdlPrismKernelFixture fixture = new();
        Vector4[] actual = Render(0), textured = Render(8);
        Assert.All(actual, v => Assert.InRange(v.W, alpha - .002f, alpha + .002f));
        Changed(actual, source, .02f);
        Changed(actual, textured, .005f);
        Vector4[] Render(float texture) => Run(fixture, source, size, size, 18, PrismFilterId.Fresco,
            [(0, 1, 1), (5, 2, 0), (10, 0, 2), (15, 3, 3)], u =>
            {
                u[24] = new(3, 0, 0, 0); u[25] = new(8, 0, 0, 0); u[26] = new(texture, 0, 0, 0);
            });
    }

    [SdlNativeTheory]
    [InlineData(PrismFilterId.AccentedEdges)]
    [InlineData(PrismFilterId.DarkStrokes)]
    [InlineData(PrismFilterId.InkOutlines)]
    public void XDogMatchesTheCpuReference(PrismFilterId filter)
    {
        const int width = 31, height = 19;
        const float alpha = .7f;
        float option0 = filter switch { PrismFilterId.AccentedEdges => 0, PrismFilterId.InkOutlines => 20, _ => 5 };
        float option1 = filter == PrismFilterId.InkOutlines ? 10 : filter == PrismFilterId.AccentedEdges ? 2 : 6;
        float option2 = filter == PrismFilterId.InkOutlines ? 4 : filter == PrismFilterId.AccentedEdges ? 5 : 2;
        PrismPremultipliedColor[] cpuSource = Enumerable.Range(0, width * height).Select(i =>
        {
            double value = i % width < width / 2 ? .2 : .8;
            return PrismPremultipliedColor.FromStraight(value, value, value, alpha);
        }).ToArray();
        Vector4[] source = cpuSource.Select(V).ToArray();
        using SdlPrismKernelFixture fixture = new();
        Vector4[] actual = Run(fixture, source, width, height, 21, filter, [(1, 4, 4), (6, 4, 4), (8, 4, 4)], u =>
        {
            u[24] = new(option0, 0, 0, 0); u[25] = new(option1, 0, 0, 0);
            u[26] = new(option2, 0, 0, 0); u[27] = new(1, 1.6f, 2, 4);
        });
        PrismCatalogFilterPlan plan = Plan(filter, width, height,
            [Number(0, option0), Number(1, option1), Number(2, option2)]);
        PrismPremultipliedColor[] expected = PrismCatalogFilterMath.Apply(plan, cpuSource, width, height, PrismColorProfile.LinearSrgb);
        Assert.All(actual, v => Premultiplied(v, alpha));
        double meanDifference = actual.Select((v, i) => Math.Abs(v.X - expected[i].Red) +
            Math.Abs(v.Y - expected[i].Green) + Math.Abs(v.Z - expected[i].Blue)).Average();
        Assert.InRange(meanDifference, 0, .02);
        Changed(actual, source, .05f);
    }

    [SdlNativeFact]
    public void PosterEdgesRunsGuidedFilterQuantizationAndInkComposite()
    {
        const int width = 25, height = 17;
        const float alpha = .65f;
        Vector4[] source = GrayStep(width, height, alpha, .2f, .8f, .04f);
        using SdlPrismKernelFixture fixture = new();
        Vector4[] actual = Render(1), noInk = Render(0);
        Assert.All(actual, v => Premultiplied(v, alpha));
        Changed(actual, source, .05f);
        Assert.True(BoundaryMean(actual, width) < BoundaryMean(noInk, width));
        Vector4[] Render(float intensity) => Run(fixture, source, width, height, 20, PrismFilterId.PosterEdges,
            [(1, 2, 0), (6, 0, 2), (8, 0, 0), (13, 2, 0), (18, 0, 2), (20, 2, 2)], u =>
            {
                u[24] = new(intensity, 0, 0, 0); u[25] = new(2, 0, 0, 0); u[26] = new(4, 0, 0, 0);
            });
    }

    [SdlNativeFact]
    public void BasReliefMatchesCpuAndReversesDirectionalShading()
    {
        const int width = 25, height = 17;
        const float alpha = .65f;
        Vector4[] source = GrayStep(width, height, alpha, .2f, .8f, .04f);
        PrismPremultipliedColor[] cpuSource = Enumerable.Range(0, width * height).Select(i =>
        {
            float value = i % width < width / 2 ? .2f : .8f;
            value += (((i % width) + (i / width)) & 1) == 0 ? -.04f : .04f;
            return PrismPremultipliedColor.FromStraight(value, value, value, alpha);
        }).ToArray();
        using SdlPrismKernelFixture fixture = new();
        Vector4[] left = Render(6), right = Render(2);
        PrismCatalogFilterPlan plan = Plan(PrismFilterId.BasRelief, width, height,
        [
            new(0, PrismGraphParameterValueKind.Color, colorValue: Color.White), Number(1, 13),
            new(2, PrismGraphParameterValueKind.Color, colorValue: Color.Black),
            new(3, PrismGraphParameterValueKind.Symbol, integerValue: PrismCatalogRuntime.ResolveSymbol("LightDirection", "Left")),
            Number(4, 3)
        ]);
        PrismPremultipliedColor[] expected = PrismCatalogFilterMath.Apply(plan, cpuSource, width, height, PrismColorProfile.LinearSrgb);
        Assert.True(BoundaryMean(left, width) > BoundaryMean(right, width));
        for (int i = 0; i < left.Length; i++)
        {
            Vector4 difference = Vector4.Abs(left[i] - V(expected[i]));
            Assert.True(difference.X <= .025 && difference.Y <= .025 && difference.Z <= .025 && difference.W <= .025,
                $"BasRelief pixel {i}: {left[i]} != {V(expected[i])}");
        }
        Vector4[] Render(float direction) => Run(fixture, source, width, height, 20, PrismFilterId.BasRelief,
            [(1, 3, 0), (6, 0, 3), (8, 0, 0), (13, 3, 0), (18, 0, 3), (20, 1, 1)], u =>
            {
                u[23] = new((int)PrismFilterId.BasRelief, (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.EdgeDetection, 0);
                u[24] = Vector4.One; u[25] = new(13, 0, 0, 0); u[26] = new(0, 0, 0, 1);
                u[27] = new(direction, 0, 0, 0); u[28] = new(3, 0, 0, 0);
            });
    }

    [SdlNativeFact]
    public void CutoutRunsBoundedMeanShiftAndQuantizesOnce()
    {
        const int width = 17, height = 9;
        const float alpha = .6f, levels = 8;
        Vector4[] source = Pixels(width, height, (x, y) =>
        {
            float noise = ((((x * 3) + (y * 5)) % 7) - 3) * .018f;
            Vector3 straight = x < width / 2
                ? new(.18f + noise, .45f - noise * .5f, .72f + noise * .3f)
                : new(.78f + noise, .28f - noise * .3f, .14f - noise * .5f);
            return new(straight * alpha, alpha);
        });
        using SdlPrismKernelFixture fixture = new();
        Vector4[] actual = Render(1), opacityZero = Render(0);
        Assert.All(actual, v =>
        {
            Assert.InRange(v.W, alpha - .002f, alpha + .002f);
            foreach (float channel in new[] { v.X, v.Y, v.Z })
            {
                float scaled = channel / v.W * (levels - 1);
                Assert.InRange(MathF.Abs(scaled - MathF.Round(scaled)), 0, .01f);
            }
        });
        Changed(actual, source, .02f);
        Assert.All(opacityZero.Zip(source), p => Assert.True(Vector4.Distance(p.First, p.Second) < .003f));
        Vector4[] Render(float opacity) => Run(fixture, source, width, height, 19, PrismFilterId.Cutout,
            [(3, 4, 4), (7, 4, 4), (8, 4, 4)], u =>
            {
                u[24] = new(levels, 0, 0, 0); u[25] = new(4, 0, 0, 0); u[26] = new(3, 0, 0, 0);
                if (u[33].Z == 8) { u[0] = new(opacity, 1f / width, 1f / height, 0); }
            });
    }

    private static Vector4[] Run(SdlPrismKernelFixture fixture, Vector4[] source, int width, int height,
        int kernel, PrismFilterId filter, (float Pass, float X, float Y)[] passes, Action<SdlGpuPrismUniforms> configure) =>
        fixture.RunPasses(source, width, height, passes.Length, i =>
        {
            SdlGpuPrismUniforms u = SdlPrismKernelFixture.CreateUniforms(kernel, width, height);
            u[23] = new((int)filter, (int)PrismColorProfile.LinearSrgb, (int)PrismCatalogFilterPrimitive.Artistic, 0);
            u[33] = new(passes[i].X, passes[i].Y, passes[i].Pass, 0);
            configure(u);
            return u;
        }, null, DrawSamplingMode.Linear, SdlGpuTextureFormat.R16G16B16A16Float);

    private static PrismCatalogFilterPlan Plan(PrismFilterId filter, int width, int height, ImmutableArray<PrismGraphParameter> parameters) =>
        PrismCatalogFilterPlanner.Create(filter, parameters, PrismBlendMode.Normal, 1, Matrix3x2.Identity, new(0, 0, width, height));
    private static PrismGraphParameter Number(int slot, float value) => new(slot, PrismGraphParameterValueKind.Number, numberValue: value);
    private static Vector4 V(PrismPremultipliedColor c) => new((float)c.Red, (float)c.Green, (float)c.Blue, (float)c.Alpha);
    private static Vector4[] Pixels(int width, int height, Func<int, int, Vector4> create) =>
        Enumerable.Range(0, width * height).Select(i => SdlPrismKernelFixture.QuantizeHalf(create(i % width, i / width))).ToArray();
    private static Vector4[] GrayStep(int width, int height, float alpha, float left, float right, float noise) =>
        Pixels(width, height, (x, y) =>
        {
            float value = (x < width / 2 ? left : right) + (((x + y) & 1) == 0 ? -noise : noise);
            return new(value * alpha, value * alpha, value * alpha, alpha);
        });
    private static void Changed(Vector4[] a, Vector4[] b, float tolerance) =>
        Assert.Contains(a.Zip(b), p => Vector4.Distance(p.First, p.Second) > tolerance);
    private static float BoundaryMean(Vector4[] pixels, int width) => pixels.Where((_, i) => i % width is 11 or 12 or 13)
        .Average(v => v.W <= 0 ? 0 : v.X / v.W);
    private static void Premultiplied(Vector4 v, float alpha)
    {
        Assert.InRange(v.W, alpha - .002f, alpha + .002f);
        Assert.True(float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z));
        Assert.InRange(v.X, 0, v.W); Assert.InRange(v.Y, 0, v.W); Assert.InRange(v.Z, 0, v.W);
    }
}
