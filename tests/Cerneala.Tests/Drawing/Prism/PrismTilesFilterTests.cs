using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismTilesFilterTests
{
    private const int Width = 12;
    private const int Height = 8;

    [Fact]
    public void PlannerNormalizesControlsIntoShaderSlots()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            tiles: 4,
            maximumOffset: 0.75f,
            background: new Color(255, 0, 255, 128),
            seed: 0x12345678);

        Assert.Equal(PrismCatalogFilterPrimitive.Tiling, plan.Primitive);
        Assert.Equal(new Vector4(1f, 0f, 1f, 128f / 255f), plan.Options0);
        uint fillBits = unchecked((uint)PrismCatalogRuntime.ResolveSymbol("Fill", "Background"));
        Assert.Equal((float)(fillBits & 0xffffu), plan.Options1.X);
        Assert.Equal((float)(fillBits >> 16), plan.Options1.Y);
        Assert.Equal(0.75f, plan.Options2.X);
        Assert.Equal(0x5678, plan.Options3.X);
        Assert.Equal(0x1234, plan.Options3.Y);
        Assert.Equal(4f, plan.Options4.X);
    }

    [Fact]
    public void ZeroOffsetIsIdentity()
    {
        PrismPremultipliedColor[] source = CreateGradient();

        PrismPremultipliedColor[] result = PrismCatalogFilterMath.Apply(
            CreatePlan(tiles: 4, maximumOffset: 0, background: Color.Transparent, seed: 17),
            source,
            Width,
            Height,
            PrismColorProfile.LinearSrgb);

        for (int index = 0; index < source.Length; index++)
        {
            Assert.Equal(source[index].Red, result[index].Red, 6);
            Assert.Equal(source[index].Green, result[index].Green, 6);
            Assert.Equal(source[index].Blue, result[index].Blue, 6);
            Assert.Equal(source[index].Alpha, result[index].Alpha, 6);
        }
    }

    [Fact]
    public void InverseCellRemapIsDeterministicAndFillsExposedPixels()
    {
        PrismPremultipliedColor[] source = CreateGradient();
        Color background = new(255, 0, 255, 128);
        PrismCatalogFilterPlan plan = CreatePlan(tiles: 3, maximumOffset: 0.8f, background, seed: 0x12345678);

        PrismPremultipliedColor[] first = PrismCatalogFilterMath.Apply(
            plan,
            source,
            Width,
            Height,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] second = PrismCatalogFilterMath.Apply(
            plan,
            source,
            Width,
            Height,
            PrismColorProfile.LinearSrgb);
        PrismPremultipliedColor[] differentSeed = PrismCatalogFilterMath.Apply(
            CreatePlan(tiles: 3, maximumOffset: 0.8f, background, seed: 0x10203040),
            source,
            Width,
            Height,
            PrismColorProfile.LinearSrgb);

        PrismPremultipliedColor expectedBackground =
            PrismPremultipliedColor.FromStraight(1, 0, 1, 128d / 255d);

        Assert.Equal(first, second);
        Assert.False(first.SequenceEqual(source));
        Assert.False(first.SequenceEqual(differentSeed));
        Assert.Contains(first, color =>
            Math.Abs(color.Red - expectedBackground.Red) < 0.000001 &&
            Math.Abs(color.Green - expectedBackground.Green) < 0.000001 &&
            Math.Abs(color.Blue - expectedBackground.Blue) < 0.000001 &&
            Math.Abs(color.Alpha - expectedBackground.Alpha) < 0.000001);
        Assert.All(first, color =>
        {
            Assert.True(double.IsFinite(color.Red));
            Assert.True(double.IsFinite(color.Green));
            Assert.True(double.IsFinite(color.Blue));
            Assert.True(double.IsFinite(color.Alpha));
            Assert.InRange(color.Red, 0, color.Alpha);
            Assert.InRange(color.Green, 0, color.Alpha);
            Assert.InRange(color.Blue, 0, color.Alpha);
        });
    }

    private static PrismCatalogFilterPlan CreatePlan(
        float tiles,
        float maximumOffset,
        Color background,
        int seed) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Tiles,
            [
                ColorParameter(0, background),
                Symbol(1, "Background"),
                Number(2, maximumOffset),
                Integer(3, seed),
                Number(4, tiles),
            ],
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, Width, Height));

    private static PrismPremultipliedColor[] CreateGradient()
    {
        PrismPremultipliedColor[] source = new PrismPremultipliedColor[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                source[(y * Width) + x] = PrismPremultipliedColor.FromStraight(
                    x / (double)(Width - 1),
                    y / (double)(Height - 1),
                    (x + y) / (double)(Width + Height - 2),
                    1);
            }
        }

        return source;
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Symbol(int slot, string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol("Fill", value));

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);
}
