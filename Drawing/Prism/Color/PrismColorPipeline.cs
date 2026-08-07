using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Drawing.Prism.ColorManagement;

internal static class PrismColorPipeline
{
    public const string AlphaConvention = "premultiplied";

    public static PrismPremultipliedColor ConvertInputToWorking(
        PrismPremultipliedColor source,
        PrismColorProfile targetProfile) =>
        ConvertInputToWorking(
            source,
            PrismColorProfile.Srgb,
            targetProfile);

    public static PrismPremultipliedColor ConvertInputToWorking(
        PrismPremultipliedColor source,
        PrismColorProfile sourceProfile,
        PrismColorProfile targetProfile)
    {
        Validate(source);
        if (source.Alpha == 0)
        {
            return default;
        }

        PrismColorChannels straight = new(
            source.Red / source.Alpha,
            source.Green / source.Alpha,
            source.Blue / source.Alpha);
        PrismColorChannels linear = DecodeProfile(
            straight,
            sourceProfile);
        PrismColorChannels converted = EncodeProfile(
            linear,
            targetProfile);

        return Associate(converted, source.Alpha);
    }

    public static PrismPremultipliedColor ConvertWorkingToOutput(
        PrismPremultipliedColor source,
        PrismColorProfile sourceProfile) =>
        ConvertWorkingToOutput(
            source,
            sourceProfile,
            PrismColorProfile.Srgb);

    public static PrismPremultipliedColor ConvertWorkingToOutput(
        PrismPremultipliedColor source,
        PrismColorProfile sourceProfile,
        PrismColorProfile targetProfile)
    {
        Validate(source);
        if (source.Alpha == 0)
        {
            return default;
        }

        PrismColorChannels straight = new(
            source.Red / source.Alpha,
            source.Green / source.Alpha,
            source.Blue / source.Alpha);
        PrismColorChannels linear = DecodeProfile(
            straight,
            sourceProfile);
        PrismColorChannels converted = EncodeProfile(
            linear,
            targetProfile);

        return Associate(converted, source.Alpha);
    }

    private static PrismColorChannels DecodeProfile(
        PrismColorChannels value,
        PrismColorProfile profile) =>
        profile switch
        {
            PrismColorProfile.LinearSrgb =>
                PrismLinearSrgbStyle.DecodeInput(value),
            PrismColorProfile.Srgb =>
                PrismSrgbStyle.DecodeInput(value),
            PrismColorProfile.LinearDisplayP3 =>
                PrismLinearDisplayP3Style.DecodeInput(value),
            PrismColorProfile.DisplayP3 =>
                PrismDisplayP3Style.DecodeInput(value),
            PrismColorProfile.ScRgb =>
                PrismScRgbStyle.DecodeInput(value),
            _ => throw new ArgumentOutOfRangeException(
                nameof(profile),
                profile,
                "Unknown Prism color profile.")
        };

    private static PrismColorChannels EncodeProfile(
        PrismColorChannels value,
        PrismColorProfile profile) =>
        profile switch
        {
            PrismColorProfile.LinearSrgb =>
                PrismLinearSrgbStyle.EncodeOutput(value),
            PrismColorProfile.Srgb =>
                PrismSrgbStyle.EncodeOutput(value),
            PrismColorProfile.LinearDisplayP3 =>
                PrismLinearDisplayP3Style.EncodeOutput(value),
            PrismColorProfile.DisplayP3 =>
                PrismDisplayP3Style.EncodeOutput(value),
            PrismColorProfile.ScRgb =>
                PrismScRgbStyle.EncodeOutput(value),
            _ => throw new ArgumentOutOfRangeException(
                nameof(profile),
                profile,
                "Unknown Prism color profile.")
        };

    internal static PrismColorChannels DecodeSrgb(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            DecodeSrgb(value.Red),
            DecodeSrgb(value.Green),
            DecodeSrgb(value.Blue));
    }

    internal static PrismColorChannels EncodeSrgb(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            EncodeSrgb(value.Red),
            EncodeSrgb(value.Green),
            EncodeSrgb(value.Blue));
    }

    internal static PrismColorChannels LinearSrgbToLinearDisplayP3(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            (0.8225927346 * value.Red) +
                (0.1775339539 * value.Green) +
                (0.0000000268 * value.Blue),
            (0.0331996005 * value.Red) +
                (0.9667835234 * value.Green) -
                (0.0000000016 * value.Blue),
            (0.0170853489 * value.Red) +
                (0.0723957406 * value.Green) +
                (0.9103014762 * value.Blue));
    }

    internal static PrismColorChannels LinearDisplayP3ToLinearSrgb(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            (1.2247454855 * value.Red) -
                (0.2249044390 * value.Green) -
                (0.0000000365 * value.Blue),
            (-0.0420580822 * value.Red) +
                (1.0420809964 * value.Green) +
                (0.0000000030 * value.Blue),
            (-0.0196422596 * value.Red) -
                (0.0786548815 * value.Green) +
                (1.0985371622 * value.Blue));
    }

    internal static PrismColorChannels Clamp01(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            Math.Clamp(value.Red, 0, 1),
            Math.Clamp(value.Green, 0, 1),
            Math.Clamp(value.Blue, 0, 1));
    }

    private static PrismPremultipliedColor Associate(
        PrismColorChannels value,
        double alpha)
    {
        return new PrismPremultipliedColor(
            value.Red * alpha,
            value.Green * alpha,
            value.Blue * alpha,
            alpha);
    }

    private static double DecodeSrgb(double value)
    {
        double magnitude = Math.Abs(value);
        return magnitude <= 0.04045
            ? value / 12.92
            : Math.CopySign(
                Math.Pow((magnitude + 0.055) / 1.055, 2.4),
                value);
    }

    private static double EncodeSrgb(double value)
    {
        double magnitude = Math.Abs(value);
        return magnitude <= 0.0031308
            ? value * 12.92
            : Math.CopySign(
                (1.055 * Math.Pow(magnitude, 1 / 2.4)) - 0.055,
                value);
    }

    private static void Validate(PrismPremultipliedColor color)
    {
        if (!double.IsFinite(color.Red) ||
            !double.IsFinite(color.Green) ||
            !double.IsFinite(color.Blue) ||
            !double.IsFinite(color.Alpha) ||
            color.Alpha < 0 ||
            color.Alpha > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(color),
                "Prism colors require finite channels and alpha in [0, 1].");
        }
    }

}

internal readonly record struct PrismColorChannels(
    double Red,
    double Green,
    double Blue);

internal readonly record struct PrismPremultipliedColor(
    double Red,
    double Green,
    double Blue,
    double Alpha)
{
    public static PrismPremultipliedColor FromStraight(
        double red,
        double green,
        double blue,
        double alpha)
    {
        return new PrismPremultipliedColor(
            red * alpha,
            green * alpha,
            blue * alpha,
            alpha);
    }
}
