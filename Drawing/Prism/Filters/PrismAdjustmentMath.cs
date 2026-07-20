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
        Vector2? pixelPosition = null)
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
            pixelPosition ?? Vector2.Zero);
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

    internal static Vector3 ApplyCurve(
        Vector3 color,
        int curve,
        int channel)
    {
        Vector3 result = color;
        if (channel == 0 || channel == 1)
        {
            result.X = EvaluateCurve(result.X, curve);
        }
        if (channel == 0 || channel == 2)
        {
            result.Y = EvaluateCurve(result.Y, curve);
        }
        if (channel == 0 || channel == 3)
        {
            result.Z = EvaluateCurve(result.Z, curve);
        }
        return result;
    }

    internal static Vector3 ApplyLookup(
        Vector3 color,
        Func<Vector3, Vector3> lookup,
        float intensity) =>
        Vector3.Lerp(color, lookup(Clamp01(color)), intensity);

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

    internal static Vector3 Threshold(
        Vector3 color,
        float level)
    {
        float value = Luminance(color) >= level ? 1 : 0;
        return new Vector3(value);
    }

    internal static Vector3 Levels(
        Vector3 color,
        int channel,
        float inputBlack,
        float inputWhite,
        float gamma,
        float outputBlack,
        float outputWhite)
    {
        float denominator =
            MathF.Max(inputWhite - inputBlack, 0.000001f);
        return ApplyChannelMap(
            color,
            channel,
            value =>
            {
                float normalized =
                    Math.Clamp(
                        (value - inputBlack) / denominator,
                        0,
                        1);
                return outputBlack +
                    (MathF.Pow(
                        normalized,
                        1 / MathF.Max(gamma, 0.000001f)) *
                    (outputWhite - outputBlack));
            });
    }

    private static Vector3 ApplyStraight(
        PrismAdjustmentPlan plan,
        Vector3 color,
        Func<Vector3, Vector3>? lookup,
        Vector2 pixelPosition)
    {
        return plan.Operation switch
        {
            PrismAdjustmentOperation.BrightnessContrast =>
                BrightnessContrast(color, plan.Parameters0),
            PrismAdjustmentOperation.Levels =>
                Levels(
                    color,
                    (int)plan.Parameters0.X,
                    plan.Parameters0.Y,
                    plan.Parameters0.Z,
                    plan.Parameters0.W,
                    plan.Parameters1.X,
                    plan.Parameters1.Y),
            PrismAdjustmentOperation.Curves =>
                ApplyCurve(
                    color,
                    (int)plan.Parameters0.Y,
                    (int)plan.Parameters0.X),
            PrismAdjustmentOperation.Exposure =>
                Exposure(color, plan.Parameters0),
            PrismAdjustmentOperation.Vibrance =>
                Vibrance(color, plan.Parameters0),
            PrismAdjustmentOperation.HueSaturation =>
                HueSaturation(
                    color,
                    plan.Parameters0,
                    plan.Parameters1.X > 0.5f),
            PrismAdjustmentOperation.ColorBalance =>
                ColorBalance(plan, color),
            PrismAdjustmentOperation.BlackWhite =>
                BlackWhite(plan, color),
            PrismAdjustmentOperation.PhotoFilter =>
                PhotoFilter(plan, color),
            PrismAdjustmentOperation.ChannelMixer =>
                ChannelMixer(plan, color),
            PrismAdjustmentOperation.ColorLookup =>
                ApplyLookup(
                    color,
                    lookup ??
                        throw new InvalidOperationException(
                            "ColorLookup requires an analytic LUT callback."),
                    plan.Parameters0.X),
            PrismAdjustmentOperation.Invert =>
                Vector3.One - color,
            PrismAdjustmentOperation.Posterize =>
                Posterize(color, plan.Parameters0.X),
            PrismAdjustmentOperation.Threshold =>
                Threshold(color, plan.Parameters0.X),
            PrismAdjustmentOperation.GradientMap =>
                GradientMap(plan, color, pixelPosition),
            PrismAdjustmentOperation.SelectiveColor =>
                SelectiveColor(plan, color),
            _ => throw new InvalidOperationException(
                $"Unknown adjustment operation '{plan.Operation}'.")
        };
    }

    private static Vector3 BrightnessContrast(
        Vector3 color,
        Vector4 parameters)
    {
        float contrast = parameters.Y;
        float factor = parameters.Z > 0.5f
            ? MathF.Max(0, 1 + contrast)
            : MathF.Pow(2, contrast * 2);
        return ((color - new Vector3(0.5f)) * factor) +
            new Vector3(0.5f + parameters.X);
    }

    private static Vector3 Exposure(
        Vector3 color,
        Vector4 parameters)
    {
        Vector3 exposed =
            (color * MathF.Pow(2, parameters.X)) +
            new Vector3(parameters.Y);
        float inverseGamma =
            1 / MathF.Max(parameters.Z, 0.000001f);
        return new Vector3(
            MathF.Pow(MathF.Max(0, exposed.X), inverseGamma),
            MathF.Pow(MathF.Max(0, exposed.Y), inverseGamma),
            MathF.Pow(MathF.Max(0, exposed.Z), inverseGamma));
    }

    private static Vector3 Vibrance(
        Vector3 color,
        Vector4 parameters)
    {
        float maximum = MathF.Max(
            color.X,
            MathF.Max(color.Y, color.Z));
        float minimum = MathF.Min(
            color.X,
            MathF.Min(color.Y, color.Z));
        float current = maximum - minimum;
        float adaptive =
            parameters.X * (1 - current);
        float amount =
            1 + adaptive + parameters.Y;
        float luminance = Luminance(color);
        return new Vector3(luminance) +
            ((color - new Vector3(luminance)) * amount);
    }

    private static Vector3 HueSaturation(
        Vector3 color,
        Vector4 parameters,
        bool colorize)
    {
        Vector3 hsv = RgbToHsv(color);
        float weight = HueWeight(
            hsv.X,
            (int)parameters.X);
        if (colorize)
        {
            hsv.X = Repeat(parameters.Y / 360f);
            hsv.Y = Math.Clamp(
                0.5f + (parameters.Z * 0.5f),
                0,
                1);
            hsv.Z = Math.Clamp(
                Luminance(color) + parameters.W,
                0,
                1);
        }
        else
        {
            hsv.X = Repeat(
                hsv.X +
                ((parameters.Y / 360f) * weight));
            hsv.Y = Math.Clamp(
                hsv.Y * (1 + (parameters.Z * weight)),
                0,
                1);
            hsv.Z = Math.Clamp(
                hsv.Z + (parameters.W * weight),
                0,
                1);
        }
        return HsvToRgb(hsv);
    }

    private static Vector3 ColorBalance(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        float luminance = Luminance(color);
        float shadows = MathF.Pow(1 - luminance, 2);
        float highlights = MathF.Pow(luminance, 2);
        float midtones =
            MathF.Max(0, 1 - shadows - highlights);
        Vector3 adjusted = color +
            (ToVector3(plan.Parameters0) * shadows) +
            (ToVector3(plan.Parameters1) * midtones) +
            (ToVector3(plan.Parameters2) * highlights);
        return plan.Parameters3.X > 0.5f
            ? PreserveLuminance(adjusted, luminance)
            : adjusted;
    }

    private static Vector3 BlackWhite(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        Vector3 hsv = RgbToHsv(color);
        float sector = hsv.X * 6;
        int first = (int)MathF.Floor(sector) % 6;
        int second = (first + 1) % 6;
        float fraction = sector - MathF.Floor(sector);
        Span<float> weights = stackalloc float[6]
        {
            plan.Parameters0.X,
            plan.Parameters0.Y,
            plan.Parameters0.Z,
            plan.Parameters0.W,
            plan.Parameters1.X,
            plan.Parameters1.Y
        };
        float gray =
            Luminance(color) *
            (weights[first] +
                ((weights[second] - weights[first]) * fraction));
        if (plan.Parameters1.Z <= 0.5f)
        {
            return new Vector3(gray);
        }
        Vector3 tint = ToVector3(plan.Parameters2);
        return tint * gray;
    }

    private static Vector3 PhotoFilter(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        float luminance = Luminance(color);
        Vector3 filtered = Vector3.Lerp(
            color,
            color * ToVector3(plan.Parameters0),
            plan.Parameters1.X);
        return plan.Parameters1.Y > 0.5f
            ? PreserveLuminance(filtered, luminance)
            : filtered;
    }

    private static Vector3 ChannelMixer(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        Vector3 mixed = ApplyMatrix(
            color,
            ToVector3(plan.Parameters0),
            ToVector3(plan.Parameters1),
            ToVector3(plan.Parameters2),
            ToVector3(plan.Parameters3));
        return plan.Parameters4.X > 0.5f
            ? new Vector3(mixed.X)
            : mixed;
    }

    private static Vector3 Posterize(
        Vector3 color,
        float levels)
    {
        float maximum =
            MathF.Max(1, MathF.Round(levels) - 1);
        return new Vector3(
            MathF.Round(color.X * maximum) / maximum,
            MathF.Round(color.Y * maximum) / maximum,
            MathF.Round(color.Z * maximum) / maximum);
    }

    private static Vector3 GradientMap(
        PrismAdjustmentPlan plan,
        Vector3 color,
        Vector2 pixelPosition)
    {
        float coordinate = Luminance(color);
        if (plan.Parameters0.Z > 0.5f)
        {
            int x = (int)MathF.Floor(pixelPosition.X) & 3;
            int y = (int)MathF.Floor(pixelPosition.Y) & 3;
            int ordered = ((x & 1) << 1) |
                (y & 1) |
                ((x & 2) << 1) |
                ((y & 2) >> 1);
            coordinate = Math.Clamp(
                coordinate +
                ((ordered - 7.5f) / (16 * 255f)),
                0,
                1);
        }
        if (plan.Parameters0.Y > 0.5f)
        {
            coordinate = 1 - coordinate;
        }
        if ((int)plan.Parameters0.X == 1)
        {
            return coordinate < 0.5f
                ? Vector3.Lerp(
                    new Vector3(0.04f, 0.08f, 0.8f),
                    new Vector3(0.85f, 0.05f, 0.03f),
                    coordinate * 2)
                : Vector3.Lerp(
                    new Vector3(0.85f, 0.05f, 0.03f),
                    new Vector3(1, 0.9f, 0.05f),
                    (coordinate - 0.5f) * 2);
        }
        return new Vector3(coordinate);
    }

    private static Vector3 SelectiveColor(
        PrismAdjustmentPlan plan,
        Vector3 color)
    {
        Vector3 hsv = RgbToHsv(color);
        float sector = hsv.X * 6;
        int first = (int)MathF.Floor(sector) % 6;
        int second = (first + 1) % 6;
        float fraction = sector - MathF.Floor(sector);
        Vector4 hue = Vector4.Lerp(
            Parameter(plan, first),
            Parameter(plan, second),
            fraction);
        float maximum = MathF.Max(
            color.X,
            MathF.Max(color.Y, color.Z));
        float minimum = MathF.Min(
            color.X,
            MathF.Min(color.Y, color.Z));
        float chroma = maximum - minimum;
        float whiteWeight =
            Math.Clamp((minimum - 0.5f) * 2, 0, 1);
        float blackWeight =
            Math.Clamp((0.5f - maximum) * 2, 0, 1);
        float neutralWeight =
            Math.Clamp(1 - chroma - whiteWeight - blackWeight, 0, 1);
        Vector4 adjustment =
            (hue * chroma) +
            (plan.Parameters6 * whiteWeight) +
            (plan.Parameters7 * neutralWeight) +
            (plan.Parameters8 * blackWeight);
        float scale = plan.Parameters9.X < 0.5f
            ? MathF.Max(0.05f, 1 - minimum)
            : 1;
        Vector3 cmy = Vector3.One - color;
        cmy += new Vector3(
            adjustment.X,
            adjustment.Y,
            adjustment.Z) * scale;
        cmy += new Vector3(adjustment.W * scale);
        return Vector3.One - cmy;
    }

    private static Vector4 Parameter(
        PrismAdjustmentPlan plan,
        int index) =>
        index switch
        {
            0 => plan.Parameters0,
            1 => plan.Parameters1,
            2 => plan.Parameters2,
            3 => plan.Parameters3,
            4 => plan.Parameters4,
            5 => plan.Parameters5,
            _ => Vector4.Zero
        };

    private static float EvaluateCurve(
        float value,
        int curve) =>
        curve switch
        {
            0 => value,
            1 => MathF.Sqrt(MathF.Max(0, value)),
            2 => value * value,
            3 => value * value * (3 - (2 * value)),
            _ => throw new InvalidOperationException(
                $"Unknown curve primitive '{curve}'.")
        };

    private static Vector3 PreserveLuminance(
        Vector3 color,
        float luminance) =>
        color + new Vector3(luminance - Luminance(color));

    private static float HueWeight(
        float hue,
        int channel)
    {
        if (channel == 0)
        {
            return 1;
        }
        float center = (channel - 1) / 6f;
        float distance = MathF.Abs(hue - center);
        distance = MathF.Min(distance, 1 - distance);
        return Math.Clamp(1 - (distance * 6), 0, 1);
    }

    private static Vector3 RgbToHsv(Vector3 color)
    {
        float maximum = MathF.Max(
            color.X,
            MathF.Max(color.Y, color.Z));
        float minimum = MathF.Min(
            color.X,
            MathF.Min(color.Y, color.Z));
        float delta = maximum - minimum;
        float hue = 0;
        if (delta > 0.000001f)
        {
            if (maximum == color.X)
            {
                hue = ((color.Y - color.Z) / delta) % 6;
            }
            else if (maximum == color.Y)
            {
                hue = ((color.Z - color.X) / delta) + 2;
            }
            else
            {
                hue = ((color.X - color.Y) / delta) + 4;
            }
            hue = Repeat(hue / 6);
        }
        float saturation =
            maximum <= 0 ? 0 : delta / maximum;
        return new Vector3(hue, saturation, maximum);
    }

    private static Vector3 HsvToRgb(Vector3 hsv)
    {
        float hue = Repeat(hsv.X) * 6;
        float chroma = hsv.Z * hsv.Y;
        float x = chroma *
            (1 - MathF.Abs((hue % 2) - 1));
        Vector3 color = (int)MathF.Floor(hue) switch
        {
            0 => new Vector3(chroma, x, 0),
            1 => new Vector3(x, chroma, 0),
            2 => new Vector3(0, chroma, x),
            3 => new Vector3(0, x, chroma),
            4 => new Vector3(x, 0, chroma),
            _ => new Vector3(chroma, 0, x)
        };
        return color + new Vector3(hsv.Z - chroma);
    }

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

    private static Vector3 Clamp01(Vector3 value) =>
        Vector3.Clamp(value, Vector3.Zero, Vector3.One);

    private static Vector3 ToVector3(Vector4 value) =>
        new(value.X, value.Y, value.Z);

    private static float Luminance(Vector3 color) =>
        Vector3.Dot(
            color,
            new Vector3(0.2126f, 0.7152f, 0.0722f));

    private static float Repeat(float value) =>
        value - MathF.Floor(value);
}
