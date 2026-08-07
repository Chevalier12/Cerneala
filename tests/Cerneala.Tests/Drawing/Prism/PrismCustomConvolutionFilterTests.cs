using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismCustomConvolutionFilterTests
{
    [Fact]
    public void DirectDenseConvolutionDoesNotNormalizeAndHonorsControls()
    {
        PrismCatalogFilterPlan preserveAlpha = CreatePlan(
            scale: 0.5f,
            offset: 0.01f);
        PrismPremultipliedColor source =
            PrismPremultipliedColor.FromStraight(
                0.05,
                0.1,
                0.15,
                0.4);

        PrismPremultipliedColor preserved = Assert.Single(
            Apply(preserveAlpha, [source], _ => Vector4.One));
        PrismPremultipliedColor affected = Assert.Single(
            Apply(
                CreatePlan(
                    scale: 0.5f,
                    offset: 0.01f,
                    affectAlpha: true),
                [source],
                _ => Vector4.One));

        Assert.Equal(PrismCatalogFilterPrimitive.Convolution, preserveAlpha.Primitive);
        PrismCatalogFilterPass pass = Assert.Single(preserveAlpha.Passes);
        Assert.Equal(1, pass.RadiusX);
        Assert.Equal(1, pass.RadiusY);
        Assert.Equal(0, preserveAlpha.Options0.X);
        Assert.Equal(0, preserveAlpha.Options1.X);
        Assert.Equal(0.01f, preserveAlpha.Options3.X);
        Assert.Equal(0.5f, preserveAlpha.Options4.X);
        Assert.True(preserveAlpha.PrimaryResourceRequired);
        Assert.Equal(KernelResource, preserveAlpha.PrimaryResource);
        Assert.Equal(0.1, preserved.Red, 5);
        Assert.Equal(0.19, preserved.Green, 5);
        Assert.Equal(0.28, preserved.Blue, 5);
        Assert.Equal(0.4, preserved.Alpha, 5);
        Assert.Equal(1, affected.Alpha, 5);
    }

    [Fact]
    public void NegativeCoefficientsAreAppliedExactly()
    {
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(),
            [
                PrismPremultipliedColor.FromStraight(0.1, 0, 0, 1),
                PrismPremultipliedColor.FromStraight(0.2, 0, 0, 1),
                PrismPremultipliedColor.FromStraight(0.3, 0, 0, 1)
            ],
            DifferenceKernel);

        Assert.Equal(0.5, result[1].Red, 5);
        Assert.Equal(1, result[1].Alpha, 5);
    }

    [Theory]
    [InlineData("Clamp", 0.1)]
    [InlineData("Transparent", 0)]
    [InlineData("Wrap", 0.9)]
    [InlineData("Mirror", 0.4)]
    [InlineData("Reflect", 0.4)]
    public void EdgeModeControlsOutOfBoundsSampling(
        string edgeMode,
        double expectedRed)
    {
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(edgeMode),
            [
                PrismPremultipliedColor.FromStraight(0.1, 0, 0, 1),
                PrismPremultipliedColor.FromStraight(0.4, 0, 0, 1),
                PrismPremultipliedColor.FromStraight(0.9, 0, 0, 1)
            ],
            LeftKernel);

        Assert.Equal(expectedRed, result[0].Red, 5);
        Assert.Equal(1, result[0].Alpha, 5);
    }

    private static readonly PrismResourceId KernelResource =
        new("custom-convolution-kernel");

    private static PrismCatalogFilterPlan CreatePlan(
        string edgeMode = "Clamp",
        float scale = 1,
        float offset = 0,
        bool affectAlpha = false) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.CustomConvolution,
            [
                new(
                    0,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: affectAlpha),
                Symbol(1, "EdgeMode", edgeMode),
                new(
                    2,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: KernelResource),
                Number(3, offset),
                Number(4, scale)
            ],
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 3, 1));

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source,
        Func<Vector2, Vector4> kernel) =>
        PrismCatalogFilterMath.Apply(
            plan,
            source,
            source.Length,
            1,
            PrismColorProfile.LinearSrgb,
            primaryResource: kernel);

    private static PrismGraphParameter Number(
        int slot,
        float value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Number,
            numberValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(
                property,
                value));

    private static Vector4 DifferenceKernel(Vector2 uv) =>
        IsKernelTap(uv, 0, 1)
            ? new Vector4(-1, 0, 0, 0)
            : IsKernelTap(uv, 1, 1)
                ? new Vector4(3, 0, 0, 0)
                : Vector4.Zero;

    private static Vector4 LeftKernel(Vector2 uv) =>
        IsKernelTap(uv, 0, 1)
            ? Vector4.UnitX
            : Vector4.Zero;

    private static bool IsKernelTap(
        Vector2 uv,
        int x,
        int y) =>
        MathF.Abs(uv.X - ((x + 0.5f) / 3)) < 0.01f &&
        MathF.Abs(uv.Y - ((y + 0.5f) / 3)) < 0.01f;
}
