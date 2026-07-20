using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Blending;

internal static class PrismBlendMath
{
    public static PrismPremultipliedColor Composite(
        PrismBlendMode mode,
        PrismPremultipliedColor source,
        PrismPremultipliedColor backdrop,
        PrismBlendOptions options,
        int pixelX = 0,
        int pixelY = 0)
    {
        Validate(mode, source, backdrop, options);
        PrismBlendColor sourceStraight = Unassociate(source);
        PrismBlendColor backdropStraight = Unassociate(backdrop);
        double blendIf = EvaluateBlendRange(
                SelectChannel(sourceStraight, options.BlendIfChannel),
                options.ThisLayerRange) *
            EvaluateBlendRange(
                SelectChannel(backdropStraight, options.BlendIfChannel),
                options.UnderlyingRange);
        PrismPremultipliedColor gatedSource =
            Scale(source, blendIf);

        PrismPremultipliedColor composite;
        if (mode == PrismBlendMode.Dissolve)
        {
            int seed = NormalizeDissolveSeed(
                options.DissolveSeed,
                options.LayerIdentity);
            bool selected = DissolveValue(
                pixelX,
                pixelY,
                seed) < gatedSource.Alpha;
            PrismPremultipliedColor dissolved = selected
                ? PrismPremultipliedColor.FromStraight(
                    sourceStraight.Red,
                    sourceStraight.Green,
                    sourceStraight.Blue,
                    1)
                : default;
            composite = CompositeAssociated(
                PrismBlendMode.Normal,
                dissolved,
                backdrop,
                PrismKnockout.None);
        }
        else
        {
            composite = CompositeAssociated(
                mode,
                gatedSource,
                backdrop,
                options.Knockout);
        }

        return ApplyChannelMask(
            composite,
            backdrop,
            options.BlendChannels);
    }

    public static double EvaluateBlendRange(
        double value,
        PrismBlendRange range)
    {
        double black = range.BlackEnd > range.BlackStart
            ? Math.Clamp(
                (value - range.BlackStart) /
                    (range.BlackEnd - range.BlackStart),
                0,
                1)
            : value >= range.BlackStart ? 1 : 0;
        double white = range.WhiteEnd > range.WhiteStart
            ? 1 - Math.Clamp(
                (value - range.WhiteStart) /
                    (range.WhiteEnd - range.WhiteStart),
                0,
                1)
            : value <= range.WhiteStart ? 1 : 0;
        return black * white;
    }

    public static int NormalizeDissolveSeed(
        int dissolveSeed,
        int layerIdentity)
    {
        uint hash = 2166136261;
        hash = (hash ^ unchecked((uint)dissolveSeed)) * 16777619;
        hash = (hash ^ unchecked((uint)layerIdentity)) * 16777619;
        return (int)(hash & 0xffff);
    }

    private static PrismPremultipliedColor CompositeAssociated(
        PrismBlendMode mode,
        PrismPremultipliedColor source,
        PrismPremultipliedColor backdrop,
        PrismKnockout knockout)
    {
        PrismBlendColor sourceStraight = Unassociate(source);
        PrismBlendColor backdropStraight = Unassociate(backdrop);
        PrismBlendColor blended = knockout == PrismKnockout.None
            ? Blend(
                mode == PrismBlendMode.PassThrough
                    ? PrismBlendMode.Normal
                    : mode,
                backdropStraight,
                sourceStraight)
            : sourceStraight;
        double overlap = source.Alpha * backdrop.Alpha;
        return new PrismPremultipliedColor(
            (source.Red * (1 - backdrop.Alpha)) +
                (backdrop.Red * (1 - source.Alpha)) +
                (blended.Red * overlap),
            (source.Green * (1 - backdrop.Alpha)) +
                (backdrop.Green * (1 - source.Alpha)) +
                (blended.Green * overlap),
            (source.Blue * (1 - backdrop.Alpha)) +
                (backdrop.Blue * (1 - source.Alpha)) +
                (blended.Blue * overlap),
            source.Alpha + backdrop.Alpha - overlap);
    }

    private static PrismBlendColor Blend(
        PrismBlendMode mode,
        PrismBlendColor backdrop,
        PrismBlendColor source)
    {
        backdrop = Clamp01(backdrop);
        source = Clamp01(source);
        PrismBlendColor result = mode switch
        {
            PrismBlendMode.Normal => source,
            PrismBlendMode.Darken => Zip(backdrop, source, Math.Min),
            PrismBlendMode.Multiply => Zip(
                backdrop,
                source,
                static (left, right) => left * right),
            PrismBlendMode.ColorBurn => Zip(
                backdrop,
                source,
                ColorBurn),
            PrismBlendMode.LinearBurn => Zip(
                backdrop,
                source,
                static (left, right) => left + right - 1),
            PrismBlendMode.DarkerColor =>
                Luminosity(backdrop) <= Luminosity(source)
                    ? backdrop
                    : source,
            PrismBlendMode.Lighten => Zip(backdrop, source, Math.Max),
            PrismBlendMode.Screen => Zip(
                backdrop,
                source,
                static (left, right) =>
                    left + right - (left * right)),
            PrismBlendMode.ColorDodge => Zip(
                backdrop,
                source,
                ColorDodge),
            PrismBlendMode.LinearDodge => Zip(
                backdrop,
                source,
                static (left, right) => left + right),
            PrismBlendMode.LighterColor =>
                Luminosity(backdrop) >= Luminosity(source)
                    ? backdrop
                    : source,
            PrismBlendMode.Overlay => Zip(
                backdrop,
                source,
                Overlay),
            PrismBlendMode.SoftLight => Zip(
                backdrop,
                source,
                SoftLight),
            PrismBlendMode.HardLight => Zip(
                backdrop,
                source,
                static (left, right) => Overlay(right, left)),
            PrismBlendMode.VividLight => Zip(
                backdrop,
                source,
                VividLight),
            PrismBlendMode.LinearLight => Zip(
                backdrop,
                source,
                static (left, right) =>
                    left + (2 * right) - 1),
            PrismBlendMode.PinLight => Zip(
                backdrop,
                source,
                PinLight),
            PrismBlendMode.HardMix => Zip(
                backdrop,
                source,
                static (left, right) =>
                    VividLight(left, right) < 0.5 ? 0 : 1),
            PrismBlendMode.Difference => Zip(
                backdrop,
                source,
                static (left, right) => Math.Abs(left - right)),
            PrismBlendMode.Exclusion => Zip(
                backdrop,
                source,
                static (left, right) =>
                    left + right - (2 * left * right)),
            PrismBlendMode.Subtract => Zip(
                backdrop,
                source,
                static (left, right) => left - right),
            PrismBlendMode.Divide => Zip(
                backdrop,
                source,
                static (left, right) =>
                    right <= 0 ? 1 : left / right),
            PrismBlendMode.Hue => SetLuminosity(
                SetSaturation(
                    source,
                    Saturation(backdrop)),
                Luminosity(backdrop)),
            PrismBlendMode.Saturation => SetLuminosity(
                SetSaturation(
                    backdrop,
                    Saturation(source)),
                Luminosity(backdrop)),
            PrismBlendMode.Color => SetLuminosity(
                source,
                Luminosity(backdrop)),
            PrismBlendMode.Luminosity => SetLuminosity(
                backdrop,
                Luminosity(source)),
            PrismBlendMode.PassThrough => source,
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unknown Prism blend mode.")
        };
        return Clamp01(result);
    }

    private static PrismPremultipliedColor ApplyChannelMask(
        PrismPremultipliedColor composite,
        PrismPremultipliedColor backdrop,
        PrismBlendChannels channels)
    {
        PrismBlendColor compositeStraight = Unassociate(composite);
        PrismBlendColor backdropStraight = Unassociate(backdrop);
        double alpha =
            (channels & PrismBlendChannels.Alpha) != 0
            ? composite.Alpha
            : backdrop.Alpha;
        return PrismPremultipliedColor.FromStraight(
            (channels & PrismBlendChannels.Red) != 0
                ? compositeStraight.Red
                : backdropStraight.Red,
            (channels & PrismBlendChannels.Green) != 0
                ? compositeStraight.Green
                : backdropStraight.Green,
            (channels & PrismBlendChannels.Blue) != 0
                ? compositeStraight.Blue
                : backdropStraight.Blue,
            alpha);
    }

    private static PrismBlendColor SetLuminosity(
        PrismBlendColor color,
        double luminosity)
    {
        double delta = luminosity - Luminosity(color);
        return ClipColor(new PrismBlendColor(
            color.Red + delta,
            color.Green + delta,
            color.Blue + delta));
    }

    private static PrismBlendColor SetSaturation(
        PrismBlendColor color,
        double saturation)
    {
        double red = color.Red;
        double green = color.Green;
        double blue = color.Blue;
        if (Math.Max(red, Math.Max(green, blue)) ==
            Math.Min(red, Math.Min(green, blue)))
        {
            return default;
        }

        if (red <= green)
        {
            if (green <= blue)
            {
                return new PrismBlendColor(
                    0,
                    ((green - red) * saturation) / (blue - red),
                    saturation);
            }
            if (red <= blue)
            {
                return new PrismBlendColor(
                    0,
                    saturation,
                    ((blue - red) * saturation) / (green - red));
            }
            return new PrismBlendColor(
                ((red - blue) * saturation) / (green - blue),
                saturation,
                0);
        }

        if (red <= blue)
        {
            return new PrismBlendColor(
                ((red - green) * saturation) / (blue - green),
                0,
                saturation);
        }
        if (green <= blue)
        {
            return new PrismBlendColor(
                saturation,
                0,
                ((blue - green) * saturation) / (red - green));
        }
        return new PrismBlendColor(
            saturation,
            ((green - blue) * saturation) / (red - blue),
            0);
    }

    private static PrismBlendColor ClipColor(PrismBlendColor color)
    {
        double luminosity = Luminosity(color);
        double minimum = Math.Min(
            color.Red,
            Math.Min(color.Green, color.Blue));
        double maximum = Math.Max(
            color.Red,
            Math.Max(color.Green, color.Blue));
        if (minimum < 0)
        {
            double scale = luminosity / (luminosity - minimum);
            color = new PrismBlendColor(
                luminosity +
                    ((color.Red - luminosity) * scale),
                luminosity +
                    ((color.Green - luminosity) * scale),
                luminosity +
                    ((color.Blue - luminosity) * scale));
        }
        if (maximum > 1)
        {
            double scale =
                (1 - luminosity) / (maximum - luminosity);
            color = new PrismBlendColor(
                luminosity +
                    ((color.Red - luminosity) * scale),
                luminosity +
                    ((color.Green - luminosity) * scale),
                luminosity +
                    ((color.Blue - luminosity) * scale));
        }
        return color;
    }

    private static PrismBlendColor Zip(
        PrismBlendColor left,
        PrismBlendColor right,
        Func<double, double, double> operation)
    {
        return new PrismBlendColor(
            operation(left.Red, right.Red),
            operation(left.Green, right.Green),
            operation(left.Blue, right.Blue));
    }

    private static PrismBlendColor Clamp01(PrismBlendColor color)
    {
        return new PrismBlendColor(
            Math.Clamp(color.Red, 0, 1),
            Math.Clamp(color.Green, 0, 1),
            Math.Clamp(color.Blue, 0, 1));
    }

    private static PrismBlendColor Unassociate(
        PrismPremultipliedColor color)
    {
        return color.Alpha > 0
            ? new PrismBlendColor(
                color.Red / color.Alpha,
                color.Green / color.Alpha,
                color.Blue / color.Alpha)
            : default;
    }

    private static PrismPremultipliedColor Scale(
        PrismPremultipliedColor color,
        double amount)
    {
        return new PrismPremultipliedColor(
            color.Red * amount,
            color.Green * amount,
            color.Blue * amount,
            color.Alpha * amount);
    }

    private static double SelectChannel(
        PrismBlendColor color,
        PrismBlendIfChannel channel)
    {
        return channel switch
        {
            PrismBlendIfChannel.Gray => Luminosity(color),
            PrismBlendIfChannel.Red => color.Red,
            PrismBlendIfChannel.Green => color.Green,
            PrismBlendIfChannel.Blue => color.Blue,
            _ => throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "Unknown Blend If channel.")
        };
    }

    private static double Luminosity(PrismBlendColor color)
    {
        return (0.3 * color.Red) +
            (0.59 * color.Green) +
            (0.11 * color.Blue);
    }

    private static double Saturation(PrismBlendColor color)
    {
        return Math.Max(
                color.Red,
                Math.Max(color.Green, color.Blue)) -
            Math.Min(
                color.Red,
                Math.Min(color.Green, color.Blue));
    }

    private static double ColorBurn(double backdrop, double source)
    {
        return source <= 0
            ? 0
            : 1 - Math.Min(1, (1 - backdrop) / source);
    }

    private static double ColorDodge(double backdrop, double source)
    {
        return source >= 1
            ? 1
            : Math.Min(1, backdrop / (1 - source));
    }

    private static double Overlay(double backdrop, double source)
    {
        return backdrop <= 0.5
            ? 2 * backdrop * source
            : 1 - (2 * (1 - backdrop) * (1 - source));
    }

    private static double SoftLight(double backdrop, double source)
    {
        if (source <= 0.5)
        {
            return backdrop -
                ((1 - (2 * source)) * backdrop * (1 - backdrop));
        }

        double curve = backdrop <= 0.25
            ? (((16 * backdrop) - 12) * backdrop + 4) * backdrop
            : Math.Sqrt(backdrop);
        return backdrop +
            (((2 * source) - 1) * (curve - backdrop));
    }

    private static double VividLight(double backdrop, double source)
    {
        return source < 0.5
            ? ColorBurn(backdrop, 2 * source)
            : ColorDodge(backdrop, 2 * (source - 0.5));
    }

    private static double PinLight(double backdrop, double source)
    {
        return source < 0.5
            ? Math.Min(backdrop, 2 * source)
            : Math.Max(backdrop, (2 * source) - 1);
    }

    private static double DissolveValue(
        int pixelX,
        int pixelY,
        int normalizedSeed)
    {
        int value = unchecked(
            (pixelX * 17) +
            (pixelY * 131) +
            (normalizedSeed * 13));
        return (value & 255) / 256d;
    }

    private static void Validate(
        PrismBlendMode mode,
        PrismPremultipliedColor source,
        PrismPremultipliedColor backdrop,
        PrismBlendOptions options)
    {
        if (!Enum.IsDefined(typeof(PrismBlendMode), mode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unknown Prism blend mode.");
        }
        if ((options.BlendChannels & ~PrismBlendChannels.Rgba) != 0 ||
            !Enum.IsDefined(typeof(PrismKnockout), options.Knockout) ||
            !Enum.IsDefined(
                typeof(PrismBlendIfChannel),
                options.BlendIfChannel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Invalid Prism advanced blending options.");
        }
        ValidateColor(source, nameof(source));
        ValidateColor(backdrop, nameof(backdrop));
    }

    private static void ValidateColor(
        PrismPremultipliedColor color,
        string parameterName)
    {
        if (!double.IsFinite(color.Red) ||
            !double.IsFinite(color.Green) ||
            !double.IsFinite(color.Blue) ||
            !double.IsFinite(color.Alpha) ||
            color.Alpha < 0 ||
            color.Alpha > 1)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Blend colors require finite channels and alpha in [0, 1].");
        }
    }

    private readonly record struct PrismBlendColor(
        double Red,
        double Green,
        double Blue);
}

internal readonly record struct PrismBlendOptions(
    PrismBlendChannels BlendChannels,
    PrismKnockout Knockout,
    PrismBlendIfChannel BlendIfChannel,
    PrismBlendRange ThisLayerRange,
    PrismBlendRange UnderlyingRange,
    int DissolveSeed,
    int LayerIdentity)
{
    public static PrismBlendOptions Default { get; } = new(
        PrismBlendChannels.Rgba,
        PrismKnockout.None,
        PrismBlendIfChannel.Gray,
        new PrismBlendRange(0, 0, 1, 1),
        new PrismBlendRange(0, 0, 1, 1),
        DissolveSeed: 0,
        LayerIdentity: 0);
}
