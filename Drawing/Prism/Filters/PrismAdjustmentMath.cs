using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismAdjustmentMath
{
    public static PrismPremultipliedColor Apply(
        PrismAdjustmentPlan plan,
        PrismPremultipliedColor source,
        PrismColorProfile workingProfile,
        float opacity = 1,
        Func<Vector3, Vector3>? lookup = null,
        Vector2? pixelPosition = null,
        PrismHaldLut? haldLookup = null,
        PrismGradientMapLut? gradientLookup = null)
    {
        if (!float.IsFinite(opacity) ||
            opacity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(opacity),
                opacity,
                "Adjustment opacity must be finite and in [0, 1].");
        }
        if (source.Alpha == 0)
        {
            return default;
        }

        PrismPremultipliedColor linear =
            ConvertProfile(
                source,
                workingProfile,
                PrismColorProfile.LinearSrgb);
        Vector3 straight = new(
            (float)(linear.Red / linear.Alpha),
            (float)(linear.Green / linear.Alpha),
            (float)(linear.Blue / linear.Alpha));
        Vector3 adjusted = ApplyStraight(
            plan,
            straight,
            lookup,
            pixelPosition ?? Vector2.Zero,
            haldLookup,
            gradientLookup);
        adjusted = Clamp01(adjusted);
        Vector3 blended = Vector3.Lerp(
            straight,
            adjusted,
            opacity);
        PrismPremultipliedColor resultLinear =
            PrismPremultipliedColor.FromStraight(
                blended.X,
                blended.Y,
                blended.Z,
                source.Alpha);
        return ConvertProfile(
            resultLinear,
            PrismColorProfile.LinearSrgb,
            workingProfile);
    }

    private static Vector3 ApplyStraight(
        PrismAdjustmentPlan plan,
        Vector3 color,
        Func<Vector3, Vector3>? lookup,
        Vector2 pixelPosition,
        PrismHaldLut? haldLookup,
        PrismGradientMapLut? gradientLookup) =>
        plan.Operation switch
        {
            PrismAdjustmentOperation.BrightnessContrast =>
                PrismBrightnessContrastFilter.Apply(
                    color,
                    plan.Parameters0),
            PrismAdjustmentOperation.Levels =>
                PrismLevelsFilter.Apply(
                    color,
                    plan.Parameters0,
                    plan.Parameters1),
            PrismAdjustmentOperation.Curves =>
                PrismCurvesFilter.Apply(color, lookup),
            PrismAdjustmentOperation.Exposure =>
                PrismExposureFilter.Apply(
                    color,
                    plan.Parameters0,
                    plan.Parameters1),
            PrismAdjustmentOperation.Vibrance =>
                PrismVibranceFilter.Apply(
                    color,
                    plan.Parameters0,
                    plan.Parameters1),
            PrismAdjustmentOperation.HueSaturation =>
                PrismHueSaturationFilter.Apply(
                    color,
                    plan.Parameters0,
                    plan.Parameters1.X > 0.5f),
            PrismAdjustmentOperation.ColorBalance =>
                PrismColorBalanceFilter.Apply(plan, color),
            PrismAdjustmentOperation.BlackWhite =>
                PrismBlackWhiteFilter.Apply(plan, color),
            PrismAdjustmentOperation.PhotoFilter =>
                PrismPhotoFilterFilter.Apply(plan, color),
            PrismAdjustmentOperation.ChannelMixer =>
                PrismChannelMixerFilter.Apply(plan, color),
            PrismAdjustmentOperation.ColorLookup =>
                PrismColorLookupFilter.Apply(
                    color,
                    lookup,
                    haldLookup),
            PrismAdjustmentOperation.Invert =>
                PrismInvertFilter.Apply(color),
            PrismAdjustmentOperation.Posterize =>
                PrismPosterizeFilter.Apply(
                    color,
                    plan.Parameters0.X),
            PrismAdjustmentOperation.Threshold =>
                PrismThresholdFilter.Apply(
                    color,
                    plan.Parameters0.X),
            PrismAdjustmentOperation.GradientMap =>
                PrismGradientMapFilter.Apply(
                    plan,
                    color,
                    pixelPosition,
                    gradientLookup,
                    lookup),
            PrismAdjustmentOperation.SelectiveColor =>
                PrismSelectiveColorFilter.Apply(plan, color),
            _ => throw new InvalidOperationException(
                $"Unknown adjustment operation '{plan.Operation}'.")
        };

    internal static Vector3 ApplyChannelMap(
        Vector3 color,
        int channel,
        Func<float, float> map)
    {
        if (channel == 0 || channel == 1)
        {
            color.X = map(color.X);
        }
        if (channel == 0 || channel == 2)
        {
            color.Y = map(color.Y);
        }
        if (channel == 0 || channel == 3)
        {
            color.Z = map(color.Z);
        }
        return color;
    }

    internal static Vector3 ApplyMatrix(
        Vector3 color,
        Vector3 red,
        Vector3 green,
        Vector3 blue,
        Vector3 constant) =>
        new(
            Vector3.Dot(color, red) + constant.X,
            Vector3.Dot(color, green) + constant.Y,
            Vector3.Dot(color, blue) + constant.Z);

    internal static Vector3 Clamp01(Vector3 value) =>
        Vector3.Clamp(value, Vector3.Zero, Vector3.One);

    internal static float Luminance(Vector3 color) =>
        Vector3.Dot(
            color,
            new Vector3(0.2126f, 0.7152f, 0.0722f));

    internal static float Repeat(float value) =>
        value - MathF.Floor(value);

    internal static float SmoothStep(
        float edge0,
        float edge1,
        float value)
    {
        float amount = Math.Clamp(
            (value - edge0) / (edge1 - edge0),
            0,
            1);
        return amount * amount * (3 - (2 * amount));
    }

    internal static Vector3 ToVector3(Vector4 value) =>
        new(value.X, value.Y, value.Z);

    internal static PrismPremultipliedColor ConvertProfile(
        PrismPremultipliedColor source,
        PrismColorProfile sourceProfile,
        PrismColorProfile targetProfile)
    {
        if (sourceProfile == targetProfile)
        {
            return source;
        }
        PrismPremultipliedColor output =
            PrismColorPipeline.ConvertWorkingToOutput(
                source,
                sourceProfile);
        return PrismColorPipeline.ConvertInputToWorking(
            output,
            targetProfile);
    }
}
