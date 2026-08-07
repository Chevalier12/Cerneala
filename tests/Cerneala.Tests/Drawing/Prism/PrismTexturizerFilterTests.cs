using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismTexturizerFilterTests
{
    [Fact]
    public void PlannerCreatesDedicatedScaleAwareDirectPass()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            texture: "Burlap",
            scaling: 2.5f,
            relief: 0.3f,
            lightDirection: "BottomLeft",
            invert: true,
            pixelScale: 2);

        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        Assert.Equal(PrismCatalogFilterPassKind.Direct, pass.Kind);
        Assert.Equal(0, pass.RadiusX);
        Assert.Equal(0, pass.RadiusY);
        Assert.Equal(1, plan.Options0.X);
        Assert.Equal(5, plan.Options1.X);
        Assert.Equal(0.3f, plan.Options2.X);
        Assert.Equal(2.5f, plan.Options3.X);
        Assert.Equal(2, plan.Options4.X);
        Assert.Equal(2, plan.Options6.X);
    }

    [Fact]
    public void ScharrLightingUsesEveryControlDeterministically()
    {
        const int width = 36;
        const int height = 28;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        PrismPremultipliedColor[] baseline = Apply(
            CreatePlan(relief: 0.25f),
            source,
            width,
            height);

        Assert.Equal(
            baseline,
            Apply(CreatePlan(relief: 0.25f), source, width, height));
        AssertDifferent(
            baseline,
            Apply(CreatePlan(texture: "Brick", relief: 0.25f), source, width, height));
        AssertDifferent(
            baseline,
            Apply(CreatePlan(scaling: 2, relief: 0.25f), source, width, height));
        AssertDifferent(
            baseline,
            Apply(CreatePlan(relief: 0.5f), source, width, height));
        AssertDifferent(
            baseline,
            Apply(CreatePlan(relief: 0.25f, lightDirection: "Right"), source, width, height));
        AssertDifferent(
            baseline,
            Apply(CreatePlan(relief: 0.25f, invert: true), source, width, height));
    }

    [Fact]
    public void CustomTextureOverridesBuiltInTextureAndPreservesAssociatedAlpha()
    {
        const int width = 24;
        const int height = 18;
        PrismPremultipliedColor[] source = CreateSubject(width, height);
        static Vector4 Texture(Vector2 uv)
        {
            float value = 0.5f + (0.25f * MathF.Sin(uv.X * 18)) +
                (0.2f * MathF.Cos(uv.Y * 14));
            value = Math.Clamp(value, 0, 1);
            return new Vector4(value, value, value, 1);
        }

        PrismPremultipliedColor[] canvas = Apply(
            CreatePlan(texture: "Canvas", relief: 0.3f),
            source,
            width,
            height,
            Texture);
        PrismPremultipliedColor[] brick = Apply(
            CreatePlan(texture: "Brick", relief: 0.3f),
            source,
            width,
            height,
            Texture);

        Assert.Equal(canvas, brick);
        for (int index = 0; index < canvas.Length; index++)
        {
            Assert.Equal(source[index].Alpha, canvas[index].Alpha, 6);
            AssertFiniteAssociated(canvas[index]);
        }
    }

    [Fact]
    public void ZeroReliefHasNoVisibleEffect()
    {
        const int width = 12;
        const int height = 9;
        PrismPremultipliedColor[] source = CreateSubject(width, height);

        PrismPremultipliedColor[] result =
            Apply(CreatePlan(relief: 0), source, width, height);

        for (int index = 0; index < source.Length; index++)
        {
            Assert.InRange(Math.Abs(source[index].Red - result[index].Red), 0, 0.000001);
            Assert.InRange(Math.Abs(source[index].Green - result[index].Green), 0, 0.000001);
            Assert.InRange(Math.Abs(source[index].Blue - result[index].Blue), 0, 0.000001);
            Assert.InRange(Math.Abs(source[index].Alpha - result[index].Alpha), 0, 0.000001);
        }
    }

    private static PrismPremultipliedColor[] Apply(
        PrismCatalogFilterPlan plan,
        PrismPremultipliedColor[] source,
        int width,
        int height,
        Func<Vector2, Vector4>? texture = null) =>
        PrismCatalogFilterMath.Apply(
            plan,
            source,
            width,
            height,
            PrismColorProfile.LinearSrgb,
            primaryResource: texture);

    private static PrismCatalogFilterPlan CreatePlan(
        string texture = "Canvas",
        float scaling = 1,
        float relief = 0.04f,
        string lightDirection = "Top",
        bool invert = false,
        float pixelScale = 1) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Texturizer,
            [
                new(0, PrismGraphParameterValueKind.Boolean, booleanValue: invert),
                Symbol(1, "LightDirection", lightDirection),
                Number(2, relief),
                Number(3, scaling),
                Symbol(4, "Texture", texture),
                new(5, PrismGraphParameterValueKind.Resource)
            ],
            PrismBlendMode.Normal,
            pixelScale,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 36, 28));

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

    private static PrismPremultipliedColor[] CreateSubject(int width, int height)
    {
        PrismPremultipliedColor[] source =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double alpha = 0.25 + (0.7 * y / Math.Max(height - 1d, 1));
                source[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        0.35 + (0.35 * x / Math.Max(width - 1d, 1)),
                        0.3 + (0.25 * y / Math.Max(height - 1d, 1)),
                        0.28,
                        alpha);
            }
        }
        source[0] = default;
        return source;
    }

    private static void AssertDifferent(
        PrismPremultipliedColor[] left,
        PrismPremultipliedColor[] right) =>
        Assert.False(left.SequenceEqual(right));

    private static void AssertFiniteAssociated(PrismPremultipliedColor color)
    {
        Assert.True(double.IsFinite(color.Red));
        Assert.True(double.IsFinite(color.Green));
        Assert.True(double.IsFinite(color.Blue));
        Assert.True(double.IsFinite(color.Alpha));
        Assert.InRange(color.Red, 0, color.Alpha);
        Assert.InRange(color.Green, 0, color.Alpha);
        Assert.InRange(color.Blue, 0, color.Alpha);
    }
}
