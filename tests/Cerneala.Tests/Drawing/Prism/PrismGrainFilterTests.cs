using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismGrainFilterTests
{
    [Fact]
    public void PlannerEncodesScaleAwarePhysicalGrainSettings()
    {
        PrismCatalogFilterPlan plan = CreatePlan(40, 50, "Regular", 17);
        PrismCatalogFilterPlan scaled = CreatePlan(
            40,
            50,
            "Regular",
            17,
            pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        AssertVector(plan.Options4, new Vector4(0.4f, 0.5f, 0, 2.875f));
        AssertVector(plan.Options5, new Vector4(1.15f, 1.15f, 0.2f, 1));
        Assert.Equal(5.75f, scaled.Options4.W, 0.00001f);
        Assert.Equal(2.3f, scaled.Options5.X, 0.00001f);
    }

    [Fact]
    public void SeedAndGrainTypeChangeARepeatableBooleanField()
    {
        const int width = 64;
        const int height = 40;
        PrismPremultipliedColor[] source = CreateFlat(width, height, 0.5);
        PrismPremultipliedColor[] first = Apply(
            CreatePlan(70, 65, "Regular", 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] repeated = Apply(
            CreatePlan(70, 65, "Regular", 91),
            source,
            width,
            height);
        PrismPremultipliedColor[] seeded = Apply(
            CreatePlan(70, 65, "Regular", 92),
            source,
            width,
            height);
        PrismPremultipliedColor[] horizontal = Apply(
            CreatePlan(70, 65, "Horizontal", 91),
            source,
            width,
            height);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(seeded));
        Assert.False(first.SequenceEqual(horizontal));
    }

    [Fact]
    public void ZeroIntensityIsIdentityAndGrainVarianceDependsOnSignal()
    {
        const int width = 72;
        const int height = 36;
        PrismPremultipliedColor[] dark = CreateFlat(width, height, 0.08);
        PrismPremultipliedColor[] middle = CreateFlat(width, height, 0.5);
        PrismCatalogFilterPlan disabled = CreatePlan(0, 80, "Regular", 1937);
        PrismCatalogFilterPlan enabled = CreatePlan(80, 80, "Regular", 1937);

        PrismPremultipliedColor[] disabledResult = Apply(
            disabled,
            dark,
            width,
            height);
        for (int index = 0; index < dark.Length; index++)
        {
            Assert.InRange(
                Math.Abs(disabledResult[index].Red - dark[index].Red),
                0,
                0.0000001);
            Assert.InRange(
                Math.Abs(disabledResult[index].Green - dark[index].Green),
                0,
                0.0000001);
            Assert.InRange(
                Math.Abs(disabledResult[index].Blue - dark[index].Blue),
                0,
                0.0000001);
            Assert.Equal(dark[index].Alpha, disabledResult[index].Alpha);
        }
        PrismPremultipliedColor[] darkResult = Apply(
            enabled,
            dark,
            width,
            height);
        PrismPremultipliedColor[] middleResult = Apply(
            enabled,
            middle,
            width,
            height);

        Assert.True(
            MeanAbsoluteDelta(middle, middleResult) >
            MeanAbsoluteDelta(dark, darkResult) * 1.15);
        Assert.Contains(
            middleResult,
            color => color.Red < 0.45);
        Assert.Contains(
            middleResult,
            color => color.Red > 0.55);
        Assert.All(middleResult, AssertFiniteAssociated);
    }

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source,
        int width,
        int height) =>
        PrismCatalogFilterMath.Apply(
            plan,
            source,
            width,
            height,
            PrismColorProfile.LinearSrgb);

    private static PrismCatalogFilterPlan CreatePlan(
        float intensity,
        float contrast,
        string type,
        int seed,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Grain,
            [
                Number(0, contrast),
                Number(1, intensity),
                Integer(2, seed),
                Symbol(3, "Type", type)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 72, 40));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

    private static PrismPremultipliedColor[] CreateFlat(
        int width,
        int height,
        double value) =>
        Enumerable.Repeat(
            PrismPremultipliedColor.FromStraight(value, value, value, 1),
            width * height)
        .ToArray();

    private static double MeanAbsoluteDelta(
        PrismPremultipliedColor[] source,
        PrismPremultipliedColor[] result) =>
        source.Zip(result, (before, after) => Math.Abs(after.Red - before.Red))
            .Average();

    private static void AssertFiniteAssociated(PrismPremultipliedColor color)
    {
        Assert.True(double.IsFinite(color.Red));
        Assert.True(double.IsFinite(color.Green));
        Assert.True(double.IsFinite(color.Blue));
        Assert.True(double.IsFinite(color.Alpha));
        Assert.InRange(color.Alpha, 0, 1);
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }

    private static void AssertVector(Vector4 actual, Vector4 expected)
    {
        Assert.Equal(expected.X, actual.X, 0.00001f);
        Assert.Equal(expected.Y, actual.Y, 0.00001f);
        Assert.Equal(expected.Z, actual.Z, 0.00001f);
        Assert.Equal(expected.W, actual.W, 0.00001f);
    }
}
