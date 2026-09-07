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
public sealed class PrismChromaticAberrationGpuTests
{
    [SdlNativeFact]
    public void GpuMatchesRadialLinearCpuFallback()
    {
        const int width = 8;
        const int height = 6;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSource(width, height);
        PrismCatalogFilterPlan plan = CreatePlan(width, height);
        Assert.Single(plan.Passes);
        Vector4[] gpuPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        PrismPremultipliedColor[] cpuPixels = PrismCatalogFilterMath.Apply(
            plan,
            sourcePixels.Select(ToPrismColor).ToArray(),
            width,
            height,
            PrismColorProfile.LinearSrgb);

        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index];
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.004);
            Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.004);
            Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.004);
            Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.004);
        }
    }

    private static Vector4[] CreateSource(int width, int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.35f + (0.05f * x) + (0.025f * y);
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    (0.05f + (0.08f * x)) * alpha,
                    (0.1f + (0.1f * y)) * alpha,
                    (0.85f - (0.07f * x)) * alpha,
                    alpha));
            }
        }
        return pixels;
    }

    private static PrismCatalogFilterPlan CreatePlan(
        int width,
        int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ChromaticAberration,
            [
                Number(0, 1.25f),
                VectorParameter(1, new System.Numerics.Vector2(0.4f, 0.55f)),
                VectorParameter(2, new System.Numerics.Vector2(0.8f, 0.6f)),
                Boolean(3, true),
                Symbol(4, "Linear")
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static PrismPremultipliedColor ToPrismColor(Vector4 value)
    {
        Vector4 color = value;
        return new PrismPremultipliedColor(
            color.X,
            color.Y,
            color.Z,
            color.W);
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter VectorParameter(
        int slot,
        System.Numerics.Vector2 value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Vector,
            vectorValue: new System.Numerics.Vector4(value, 0, 0));

    private static PrismGraphParameter Boolean(int slot, bool value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Boolean,
            booleanValue: value);

    private static PrismGraphParameter Symbol(int slot, string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(
                "Sampling",
                value));
}
