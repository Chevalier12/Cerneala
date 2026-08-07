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

public sealed class PrismColorFilterTests
{
    [Fact]
    public void NeutralGradeIsAnExactIdentity()
    {
        Vector3 source = new(0.08f, 0.42f, 0.91f);

        Vector3 result = PrismColorFilter.Apply(
            source,
            brightness: 0,
            contrast: 1,
            exposure: 0,
            saturation: 1,
            hueDegrees: 0,
            temperature: 0,
            tint: Vector4.Zero,
            clamp: true);

        Assert.Equal(source, result);
    }

    [Fact]
    public void HueRotationPreservesOklabLightnessAndChroma()
    {
        Vector3 source = new(0.12f, 0.48f, 0.22f);
        Vector3 before = PrismColorFilter.ToOklab(source);

        Vector3 result = PrismColorFilter.Apply(
            source,
            brightness: 0,
            contrast: 1,
            exposure: 0,
            saturation: 1,
            hueDegrees: 73,
            temperature: 0,
            tint: Vector4.Zero,
            clamp: false);
        Vector3 after = PrismColorFilter.ToOklab(result);

        Assert.InRange(MathF.Abs(after.X - before.X), 0, 0.00002f);
        Assert.InRange(
            MathF.Abs(
                MathF.Sqrt((after.Y * after.Y) + (after.Z * after.Z)) -
                MathF.Sqrt((before.Y * before.Y) + (before.Z * before.Z))),
            0,
            0.00002f);
    }

    [Fact]
    public void TemperatureUsesChromaticAdaptationInBothDirections()
    {
        Vector3 neutral = new(0.5f);

        Vector3 warm = ApplyWhiteBalance(neutral, 0.75f, Vector4.Zero);
        Vector3 cool = ApplyWhiteBalance(neutral, -0.75f, Vector4.Zero);

        Assert.True(warm.X > warm.Z);
        Assert.True(cool.Z > cool.X);
        Assert.NotEqual(neutral.Y, warm.Y);
        Assert.NotEqual(neutral.Y, cool.Y);
    }

    [Fact]
    public void TintAlphaControlsCat16DestinationWhite()
    {
        Vector3 neutral = new(0.4f);
        Vector4 cyanTint = new(0, 1, 1, 0.65f);

        Vector3 unchanged = ApplyWhiteBalance(
            neutral,
            0,
            new Vector4(cyanTint.X, cyanTint.Y, cyanTint.Z, 0));
        Vector3 tinted = ApplyWhiteBalance(neutral, 0, cyanTint);

        Assert.Equal(neutral, unchanged);
        Assert.True(tinted.Z > tinted.X);
        Assert.NotEqual(Vector3.Lerp(neutral, Vector3.UnitY + Vector3.UnitZ, 0.65f), tinted);
    }

    [Fact]
    public void ExtremeChromaIsCompressedInsideLinearSrgbGamut()
    {
        Vector3 result = PrismColorFilter.Apply(
            new Vector3(0.85f, 0.12f, 0.04f),
            brightness: 0,
            contrast: 1,
            exposure: 0,
            saturation: 6,
            hueDegrees: 137,
            temperature: 0,
            tint: Vector4.Zero,
            clamp: true);

        Assert.InRange(result.X, 0, 1);
        Assert.InRange(result.Y, 0, 1);
        Assert.InRange(result.Z, 0, 1);
        Assert.True(Vector3.Distance(result, new Vector3(result.X)) > 0.01f);
    }

    [Fact]
    public void CatalogPathPreservesAlphaAndExtendedRangeWhenClampIsDisabled()
    {
        PrismCatalogFilterPlan plan = CreatePlan(
            brightness: 1,
            clamp: false);

        PrismPremultipliedColor result = Assert.Single(
            PrismCatalogFilterMath.Apply(
                plan,
                [PrismPremultipliedColor.FromStraight(0.4, 0.3, 0.2, 0.5)],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.Equal(0.5, result.Alpha, 6);
        Assert.True(result.Red > result.Alpha);
    }

    private static Vector3 ApplyWhiteBalance(
        Vector3 source,
        float temperature,
        Vector4 tint) =>
        PrismColorFilter.Apply(
            source,
            brightness: 0,
            contrast: 1,
            exposure: 0,
            saturation: 1,
            hueDegrees: 0,
            temperature,
            tint,
            clamp: false);

    private static PrismCatalogFilterPlan CreatePlan(
        float brightness = 0,
        bool clamp = true)
    {
        PrismGraphParameter[] parameters =
        [
                Number(0, brightness),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: clamp),
                Number(2, 1),
                Number(3, 0),
                Number(4, 0),
                Symbol(5, "Matrix", "Identity"),
                Number(6, 1),
                Number(7, 0),
                ColorParameter(8, new Color(0, 0, 0, 0))
        ];
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismFilterId.Color);
        Assert.Equal(entry.Properties.Length, parameters.Length);
        Assert.Equal(
            PrismCatalogValueType.Boolean,
            entry.Properties[1].ValueType);
        Assert.Equal(
            PrismGraphParameterValueKind.Boolean,
            parameters[1].Kind);
        for (int index = 0; index < parameters.Length; index++)
        {
            Assert.Equal(index, entry.Properties[index].Slot);
            Assert.Equal(index, parameters[index].Index);
        }

        return PrismCatalogFilterPlanner.Create(
            PrismFilterId.Color,
            [.. parameters],
            PrismBlendMode.Normal,
            1,
            Matrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(int slot, Color value) =>
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
