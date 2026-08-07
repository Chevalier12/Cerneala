using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismChromaticAberrationFilterTests
{
    private const int Width = 5;
    private const int Height = 3;

    [Fact]
    public void PlannerAccountsForMaximumRadialModulation()
    {
        PrismCatalogFilterPlan directional = CreatePlan(
            amount: 2,
            direction: Vector2.UnitX,
            radial: false,
            center: new Vector2(0.5f));
        PrismCatalogFilterPlan radial = CreatePlan(
            amount: 2,
            direction: Vector2.UnitX,
            radial: true,
            center: new Vector2(0.5f));

        Assert.Equal(2, Assert.Single(directional.Passes).RadiusX);
        Assert.Equal(2, Assert.Single(directional.Passes).RadiusY);
        Assert.Equal(
            2 * MathF.Sqrt(2),
            Assert.Single(radial.Passes).RadiusX,
            5);
        Assert.Equal(
            Assert.Single(radial.Passes).RadiusX,
            Assert.Single(radial.Passes).RadiusY);
    }

    [Fact]
    public void RadialModulationIsZeroAtCenterAndHonorsCenterControl()
    {
        PrismPremultipliedColor[] source = CreateSource();
        PrismPremultipliedColor[] centered = Apply(
            CreatePlan(
                amount: 1,
                direction: Vector2.UnitX,
                radial: true,
                center: new Vector2(0.5f)),
            source);
        PrismPremultipliedColor[] directional = Apply(
            CreatePlan(
                amount: 1,
                direction: Vector2.UnitX,
                radial: false,
                center: new Vector2(0.5f)),
            source);
        PrismPremultipliedColor[] shiftedCenter = Apply(
            CreatePlan(
                amount: 1,
                direction: Vector2.UnitX,
                radial: true,
                center: new Vector2(0.1f, 0.5f)),
            source);

        int middle = (Height / 2 * Width) + (Width / 2);
        int leftMiddle = Height / 2 * Width;
        AssertColor(source[middle], centered[middle]);
        Assert.NotEqual(source[middle], directional[middle]);
        AssertColor(source[leftMiddle], shiftedCenter[leftMiddle]);
        Assert.NotEqual(source[leftMiddle], centered[leftMiddle]);
    }

    [Fact]
    public void UsesLinearSamplingAndMaximumSampledAlpha()
    {
        PrismPremultipliedColor[] source = CreateSource();
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(
                amount: 0.5f,
                direction: Vector2.UnitX,
                radial: false,
                center: new Vector2(0.5f)),
            source);

        int index = (Height / 2 * Width) + (Width / 2);
        PrismPremultipliedColor left = source[index - 1];
        PrismPremultipliedColor center = source[index];
        PrismPremultipliedColor right = source[index + 1];
        double expectedRed = (center.Red + right.Red) * 0.5;
        double expectedBlue = (left.Blue + center.Blue) * 0.5;
        double expectedAlpha = Math.Max(
            center.Alpha,
            Math.Max(
                (center.Alpha + right.Alpha) * 0.5,
                (left.Alpha + center.Alpha) * 0.5));

        Assert.Equal(expectedRed, result[index].Red, 6);
        Assert.Equal(center.Green, result[index].Green, 6);
        Assert.Equal(expectedBlue, result[index].Blue, 6);
        Assert.Equal(expectedAlpha, result[index].Alpha, 6);
        AssertAssociated(result[index]);
    }

    [Fact]
    public void ZeroAmountIsIdentity()
    {
        PrismPremultipliedColor[] source = CreateSource();
        PrismPremultipliedColor[] result = Apply(
            CreatePlan(
                amount: 0,
                direction: Vector2.Zero,
                radial: true,
                center: new Vector2(0.25f, 0.75f)),
            source);

        for (int index = 0; index < source.Length; index++)
        {
            AssertColor(source[index], result[index]);
        }
    }

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source) =>
        PrismCatalogFilterMath.Apply(
            plan,
            source,
            Width,
            Height,
            PrismColorProfile.LinearSrgb);

    private static PrismCatalogFilterPlan CreatePlan(
        float amount,
        Vector2 direction,
        bool radial,
        Vector2 center)
    {
        ImmutableArray<PrismGraphParameter> parameters =
        [
            Number(0, amount),
            VectorParameter(1, center),
            VectorParameter(2, direction),
            Boolean(3, radial),
            Symbol(4, "Linear")
        ];

        return PrismCatalogFilterPlanner.Create(
            PrismFilterId.ChromaticAberration,
            parameters,
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, Width, Height));
    }

    private static PrismPremultipliedColor[] CreateSource()
    {
        PrismPremultipliedColor[] pixels =
            new PrismPremultipliedColor[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                double alpha = 0.35 + (0.1 * x) + (0.05 * y);
                pixels[(y * Width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        0.1 + (0.15 * x),
                        0.15 + (0.2 * y),
                        0.8 - (0.12 * x),
                        alpha);
            }
        }
        return pixels;
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter VectorParameter(
        int slot,
        Vector2 value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Vector,
            vectorValue: new Vector4(value, 0, 0));

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

    private static void AssertColor(
        PrismPremultipliedColor expected,
        PrismPremultipliedColor actual)
    {
        Assert.Equal(expected.Red, actual.Red, 6);
        Assert.Equal(expected.Green, actual.Green, 6);
        Assert.Equal(expected.Blue, actual.Blue, 6);
        Assert.Equal(expected.Alpha, actual.Alpha, 6);
    }

    private static void AssertAssociated(PrismPremultipliedColor color)
    {
        Assert.InRange(color.Alpha, 0, 1);
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }
}
