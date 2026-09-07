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
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using NumericsVector4 = System.Numerics.Vector4;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismColorGpuTests
{
    [SdlNativeFact]
    public void ScRgbGpuKernelsPreservePremultipliedExtendedRange()
    {
        const float alpha = 0.5f;
        Vector4 expected = new(-0.25f * alpha, 2f * alpha, 7f * alpha, alpha);
        using SdlPrismKernelFixture fixture = new();
        Vector4 pixel = Assert.Single(fixture.RunKernel(2, [expected], 1, 1,
            uniforms => uniforms[23] = new Vector4(
                (int)PrismColorProfile.ScRgb, (int)PrismColorProfile.ScRgb, 0, 0)));
        Assert.InRange(Math.Abs(pixel.X - expected.X), 0, 0.003f);
        Assert.InRange(Math.Abs(pixel.Y - expected.Y), 0, 0.003f);
        Assert.InRange(Math.Abs(pixel.Z - expected.Z), 0, 0.008f);
        Assert.InRange(Math.Abs(pixel.W - expected.W), 0, 0.003f);
    }

    [SdlNativeTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void GpuMatchesCpuForOklabCat16GradeAndClampMode(bool clamp)
    {
        const float alpha = 0.55f;
        PrismCatalogFilterPlan plan = CreatePlan(clamp);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        using SdlPrismKernelFixture fixture = new();
        Vector4 gpu = Assert.Single(fixture.RunCatalog(plan,
            [new Vector4(0.72f * alpha, 0.24f * alpha, 0.08f * alpha, alpha)], 1, 1));
        PrismPremultipliedColor cpu = Assert.Single(
            PrismCatalogFilterMath.Apply(
                plan,
                [PrismPremultipliedColor.FromStraight(0.72, 0.24, 0.08, alpha)],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.006f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.006f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.006f);
        Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.003f);
    }

    private static PrismCatalogFilterPlan CreatePlan(bool clamp) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Color,
            [
                Number(0, 0.08f),
                Boolean(1, clamp),
                Number(2, 1.2f),
                Number(3, 0.35f),
                Number(4, 41),
                Symbol(5, "Matrix", "Identity"),
                Number(6, 1.7f),
                Number(7, 0.3f),
                ColorParameter(8, new Cerneala.Drawing.Color(24, 190, 220, 96))
            ],
            PrismBlendMode.Normal,
            1,
            NumericsMatrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Boolean(int slot, bool value) =>
        new(slot, PrismGraphParameterValueKind.Boolean, booleanValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));
}
