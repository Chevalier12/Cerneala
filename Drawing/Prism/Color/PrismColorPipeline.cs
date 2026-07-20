using Cerneala.Drawing.Prism.Catalog;

namespace Cerneala.Drawing.Prism.ColorManagement;

internal static class PrismColorPipeline
{
    public const string AlphaConvention = "premultiplied";

    public static PrismPremultipliedColor ConvertInputToWorking(
        PrismPremultipliedColor source,
        PrismColorProfile targetProfile)
    {
        Validate(source);
        if (source.Alpha == 0)
        {
            return default;
        }

        PrismColorChannels straight = Clamp01(new PrismColorChannels(
            source.Red / source.Alpha,
            source.Green / source.Alpha,
            source.Blue / source.Alpha));
        PrismColorChannels converted = targetProfile switch
        {
            PrismColorProfile.LinearSrgb =>
                DecodeSrgb(straight),
            PrismColorProfile.Srgb =>
                straight,
            PrismColorProfile.LinearDisplayP3 =>
                Clamp01(LinearSrgbToLinearDisplayP3(
                    DecodeSrgb(straight))),
            PrismColorProfile.DisplayP3 =>
                Clamp01(EncodeSrgb(
                    LinearSrgbToLinearDisplayP3(
                        DecodeSrgb(straight)))),
            PrismColorProfile.ScRgb =>
                DecodeSrgb(straight),
            _ => throw new ArgumentOutOfRangeException(
                nameof(targetProfile),
                targetProfile,
                "Unknown Prism color profile.")
        };

        return Associate(converted, source.Alpha);
    }

    public static PrismPremultipliedColor ConvertWorkingToOutput(
        PrismPremultipliedColor source,
        PrismColorProfile sourceProfile)
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
        PrismColorChannels converted = sourceProfile switch
        {
            PrismColorProfile.LinearSrgb =>
                EncodeSrgb(straight),
            PrismColorProfile.Srgb =>
                straight,
            PrismColorProfile.LinearDisplayP3 =>
                EncodeSrgb(
                    LinearDisplayP3ToLinearSrgb(straight)),
            PrismColorProfile.DisplayP3 =>
                EncodeSrgb(
                    LinearDisplayP3ToLinearSrgb(
                        DecodeSrgb(Clamp01(straight)))),
            PrismColorProfile.ScRgb =>
                EncodeSrgb(straight),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sourceProfile),
                sourceProfile,
                "Unknown Prism color profile.")
        };

        return Associate(Clamp01(converted), source.Alpha);
    }

    private static PrismColorChannels DecodeSrgb(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            DecodeSrgb(value.Red),
            DecodeSrgb(value.Green),
            DecodeSrgb(value.Blue));
    }

    private static PrismColorChannels EncodeSrgb(
        PrismColorChannels value)
    {
        return new PrismColorChannels(
            EncodeSrgb(value.Red),
            EncodeSrgb(value.Green),
            EncodeSrgb(value.Blue));
    }

    private static PrismColorChannels LinearSrgbToLinearDisplayP3(
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

    private static PrismColorChannels LinearDisplayP3ToLinearSrgb(
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

    private static PrismColorChannels Clamp01(
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
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static double EncodeSrgb(double value)
    {
        return value <= 0.0031308
            ? value * 12.92
            : (1.055 * Math.Pow(value, 1 / 2.4)) - 0.055;
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

    private readonly record struct PrismColorChannels(
        double Red,
        double Green,
        double Blue);
}

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
