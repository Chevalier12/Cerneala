using Cerneala.Drawing;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;
using DrawingColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismTilesGpuTests
{
    [SdlNativeFact]
    public void GpuMatchesCleanRoomInverseCellRemapCpuFallback()
    {
        const int width = 8;
        const int height = 8;
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
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    (x + 1) / 10f,
                    (y + 1) / 10f,
                    (x + y + 2) / 20f,
                    1));
            }
        }

        return pixels;
    }

    private static PrismCatalogFilterPlan CreatePlan(int width, int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Tiles,
            [
                ColorParameter(0, new DrawingColor(255, 0, 255, 128)),
                Symbol(1, "Background"),
                Number(2, 0.6f),
                Integer(3, 0x12345678),
                Number(4, 2),
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

    private static PrismGraphParameter Symbol(int slot, string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol("Fill", value));

    private static PrismGraphParameter ColorParameter(int slot, DrawingColor value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);
}
