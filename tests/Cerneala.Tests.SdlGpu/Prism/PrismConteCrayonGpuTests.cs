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
public sealed class PrismConteCrayonGpuTests
{
    [SdlNativeFact]
    public void FlowXDogPipelineRendersWithDedicatedFinalEffect()
    {
        const int width = 32;
        const int height = 24;
        using SdlPrismKernelFixture fixture = new();
        Vector4[] sourcePixels = CreateSubject(width, height);
        PrismCatalogFilterPlan plan = CreatePlan();
        Vector4[] resultPixels = fixture.RunCatalog(plan, sourcePixels, width, height);
        double difference = 0;
        for (int index = 0; index < resultPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index];
            Vector4 result = resultPixels[index];
            difference +=
                Math.Abs(source.X - result.X) +
                Math.Abs(source.Y - result.Y) +
                Math.Abs(source.Z - result.Z);
            Assert.InRange(Math.Abs(source.W - result.W), 0, 0.01f);
            Assert.InRange(result.X, 0, result.W + 0.01f);
            Assert.InRange(result.Y, 0, result.W + 0.01f);
            Assert.InRange(result.Z, 0, result.W + 0.01f);
        }

        Assert.True(difference / resultPixels.Length > 0.04);
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ConteCrayon,
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                Number(1, 9),
                ColorParameter(2, Cerneala.Drawing.Color.Black),
                Number(3, 14),
                Symbol(4, "LightDirection", "BottomLeft"),
                Number(5, 0.75f),
                Number(6, 1),
                Symbol(7, "Texture", "Burlap")
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 24));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static Vector4[] CreateSubject(
        int width,
        int height)
    {
        Vector4[] pixels = new Vector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float edge = x < width / 2 ? 0.14f : 0.78f;
                float grain = ((x * 7) + (y * 11)) % 17 / 120f;
                float value = Math.Clamp(edge + grain, 0, 1);
                float alpha = (x + y) % 9 == 0 ? 0.45f : 0.86f;
                pixels[(y * width) + x] = SdlPrismKernelFixture.QuantizeHalf(new Vector4(
                    value * alpha,
                    value * 0.92f * alpha,
                    value * 0.74f * alpha,
                    alpha));
            }
        }

        return pixels;
    }
}
