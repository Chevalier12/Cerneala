using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using System.Numerics;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsVector4 = System.Numerics.Vector4;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismColorMatrixGpuTests
{
    [SdlNativeTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void GpuMatchesCpuForAffineRgbaAndClampMode(bool clamp)
    {
        const float alpha = 0.5f;
        PrismColorMatrixResource matrix = new(
            new NumericsMatrix4x4(
                0, 1, 0, 0,
                1, 0, 0, 0,
                0, 0, 1, 0.1f,
                0, 0, 0, 0.5f),
            new NumericsVector4(1.1f, -0.4f, 0, 0.1f));
        PrismCatalogFilterPlan plan = CreatePlan(clamp);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismColorMatrixFilter.Pack(
            matrix,
            out NumericsVector4 rowRed,
            out NumericsVector4 rowGreen,
            out NumericsVector4 rowBlue,
            out NumericsVector4 rowAlpha,
            out NumericsVector4 offset);

        using SdlPrismKernelFixture fixture = new();
        Vector4 gpu = Assert.Single(fixture.RunCatalog(plan,
            [new Vector4(0.2f * alpha, 0.4f * alpha, 0.6f * alpha, alpha)], 1, 1,
            configure: uniforms =>
            {
                Vector4 header = uniforms[23];
                header.W = 1;
                uniforms[23] = header;
                uniforms[26] = rowRed;
                uniforms[27] = rowGreen;
                uniforms[28] = rowBlue;
                uniforms[29] = rowAlpha;
                uniforms[30] = offset;
            }));
        PrismPremultipliedColor cpu = Assert.Single(
            PrismCatalogFilterMath.Apply(
                plan,
                [
                    PrismPremultipliedColor.FromStraight(
                        0.2,
                        0.4,
                        0.6,
                        alpha)
                ],
                1,
                1,
                PrismColorProfile.LinearSrgb,
                colorMatrixResource: matrix));

        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.003f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.003f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.003f);
        Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.003f);
    }

    private static PrismCatalogFilterPlan CreatePlan(bool clamp) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ColorMatrix,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: clamp),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: new PrismResourceId("color-matrix"))
            ],
            PrismBlendMode.Normal,
            1,
            NumericsMatrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));
}
