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
        double blendIf = PrismBlendIfStyle.Evaluate(
                PrismBlendIfStyle.SelectChannel(
                    sourceStraight,
                    options.BlendIfChannel),
                options.ThisLayerRange) *
            PrismBlendIfStyle.Evaluate(
                PrismBlendIfStyle.SelectChannel(
                    backdropStraight,
                    options.BlendIfChannel),
                options.UnderlyingRange);
        PrismPremultipliedColor gatedSource =
            Scale(source, blendIf);

        PrismPremultipliedColor composite;
        if (mode == PrismBlendMode.Dissolve)
        {
            int seed = PrismDissolveBlend.NormalizeSeed(
                options.DissolveSeed,
                options.LayerIdentity);
            bool selected = PrismDissolveBlend.IsSelected(
                pixelX,
                pixelY,
                seed,
                gatedSource.Alpha);
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
                backdrop);
        }
        else
        {
            composite = options.Knockout == PrismKnockout.None
                ? CompositeAssociated(
                    mode,
                    gatedSource,
                    backdrop)
                : CompositeKnockout(
                    mode,
                    gatedSource,
                    backdrop,
                    backdrop,
                    gatedSource.Alpha);
        }

        return PrismAdvancedBlendingStyle.ApplyChannelMask(
            composite,
            backdrop,
            options.BlendChannels);
    }

    public static double EvaluateBlendRange(
        double value,
        PrismBlendRange range)
        => PrismBlendIfStyle.Evaluate(value, range);

    public static int NormalizeDissolveSeed(
        int dissolveSeed,
        int layerIdentity) =>
        PrismDissolveBlend.NormalizeSeed(
            dissolveSeed,
            layerIdentity);

    private static PrismPremultipliedColor CompositeAssociated(
        PrismBlendMode mode,
        PrismPremultipliedColor source,
        PrismPremultipliedColor backdrop)
    {
        PrismBlendColor sourceStraight = Unassociate(source);
        PrismBlendColor backdropStraight = Unassociate(backdrop);
        PrismBlendColor blended = Blend(
            mode == PrismBlendMode.PassThrough
                ? PrismBlendMode.Normal
                : mode,
            backdropStraight,
            sourceStraight);
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


    internal static PrismPremultipliedColor CompositeKnockout(
        PrismBlendMode mode,
        PrismPremultipliedColor source,
        PrismPremultipliedColor currentBackdrop,
        PrismPremultipliedColor originalBackdrop,
        double sourceShape)
    {
        ValidateColor(source, nameof(source));
        ValidateColor(currentBackdrop, nameof(currentBackdrop));
        ValidateColor(originalBackdrop, nameof(originalBackdrop));
        if (!double.IsFinite(sourceShape) ||
            sourceShape is < 0 or > 1 ||
            source.Alpha > sourceShape)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceShape),
                "Knockout shape must be finite, in [0, 1], and no smaller than source alpha.");
        }

        PrismBlendColor sourceStraight = Unassociate(source);
        PrismBlendColor originalStraight = Unassociate(originalBackdrop);
        PrismBlendColor blended = Blend(
            mode == PrismBlendMode.PassThrough
                ? PrismBlendMode.Normal
                : mode,
            originalStraight,
            sourceStraight);
        double previousGroupAlpha = ResolveGroupAlpha(
            currentBackdrop.Alpha,
            originalBackdrop.Alpha);
        double groupAlpha =
            ((1 - sourceShape) * previousGroupAlpha) +
            source.Alpha;
        double alpha = Union(originalBackdrop.Alpha, groupAlpha);
        double uncoveredShape = sourceShape - source.Alpha;

        return new PrismPremultipliedColor(
            ((1 - sourceShape) * currentBackdrop.Red) +
                (uncoveredShape * originalBackdrop.Red) +
                (source.Alpha *
                    (((1 - originalBackdrop.Alpha) * sourceStraight.Red) +
                        (originalBackdrop.Alpha * blended.Red))),
            ((1 - sourceShape) * currentBackdrop.Green) +
                (uncoveredShape * originalBackdrop.Green) +
                (source.Alpha *
                    (((1 - originalBackdrop.Alpha) * sourceStraight.Green) +
                        (originalBackdrop.Alpha * blended.Green))),
            ((1 - sourceShape) * currentBackdrop.Blue) +
                (uncoveredShape * originalBackdrop.Blue) +
                (source.Alpha *
                    (((1 - originalBackdrop.Alpha) * sourceStraight.Blue) +
                        (originalBackdrop.Alpha * blended.Blue))),
            alpha);
    }

    private static double ResolveGroupAlpha(
        double currentAlpha,
        double originalAlpha)
    {
        if (originalAlpha >= 1)
        {
            return 0;
        }

        return Math.Clamp(
            (currentAlpha - originalAlpha) / (1 - originalAlpha),
            0,
            1);
    }

    private static double Union(double backdrop, double source) =>
        backdrop + source - (backdrop * source);

    private static PrismBlendColor Blend(
        PrismBlendMode mode,
        PrismBlendColor backdrop,
        PrismBlendColor source)
    {
        backdrop = Clamp01(backdrop);
        source = Clamp01(source);
        PrismBlendColor result = mode switch
        {
            PrismBlendMode.Normal => PrismNormalBlend.Evaluate(backdrop, source),
            PrismBlendMode.Darken => PrismDarkenBlend.Evaluate(backdrop, source),
            PrismBlendMode.Multiply => PrismMultiplyBlend.Evaluate(backdrop, source),
            PrismBlendMode.ColorBurn => PrismColorBurnBlend.Evaluate(backdrop, source),
            PrismBlendMode.LinearBurn => PrismLinearBurnBlend.Evaluate(backdrop, source),
            PrismBlendMode.DarkerColor => PrismDarkerColorBlend.Evaluate(backdrop, source),
            PrismBlendMode.Lighten => PrismLightenBlend.Evaluate(backdrop, source),
            PrismBlendMode.Screen => PrismScreenBlend.Evaluate(backdrop, source),
            PrismBlendMode.ColorDodge => PrismColorDodgeBlend.Evaluate(backdrop, source),
            PrismBlendMode.LinearDodge => PrismLinearDodgeBlend.Evaluate(backdrop, source),
            PrismBlendMode.LighterColor => PrismLighterColorBlend.Evaluate(backdrop, source),
            PrismBlendMode.Overlay => PrismOverlayBlend.Evaluate(backdrop, source),
            PrismBlendMode.SoftLight => PrismSoftLightBlend.Evaluate(backdrop, source),
            PrismBlendMode.HardLight => PrismHardLightBlend.Evaluate(backdrop, source),
            PrismBlendMode.VividLight => PrismVividLightBlend.Evaluate(backdrop, source),
            PrismBlendMode.LinearLight => PrismLinearLightBlend.Evaluate(backdrop, source),
            PrismBlendMode.PinLight => PrismPinLightBlend.Evaluate(backdrop, source),
            PrismBlendMode.HardMix => PrismHardMixBlend.Evaluate(backdrop, source),
            PrismBlendMode.Difference => PrismDifferenceBlend.Evaluate(backdrop, source),
            PrismBlendMode.Exclusion => PrismExclusionBlend.Evaluate(backdrop, source),
            PrismBlendMode.Subtract => PrismSubtractBlend.Evaluate(backdrop, source),
            PrismBlendMode.Divide => PrismDivideBlend.Evaluate(backdrop, source),
            PrismBlendMode.Hue => PrismHueBlend.Evaluate(backdrop, source),
            PrismBlendMode.Saturation => PrismSaturationBlend.Evaluate(backdrop, source),
            PrismBlendMode.Color => PrismColorBlend.Evaluate(backdrop, source),
            PrismBlendMode.Luminosity => PrismLuminosityBlend.Evaluate(backdrop, source),
            PrismBlendMode.PassThrough => PrismPassThroughBlend.Evaluate(backdrop, source),
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Unknown Prism blend mode.")
        };
        return Clamp01(result);
    }

    internal static PrismBlendColor SetLuminosity(
        PrismBlendColor color,
        double luminosity)
    {
        double delta = luminosity - Luminosity(color);
        return ClipColor(new PrismBlendColor(
            color.Red + delta,
            color.Green + delta,
            color.Blue + delta));
    }

    internal static PrismBlendColor SetSaturation(
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

    internal static PrismBlendColor Zip(
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

    internal static PrismBlendColor Unassociate(
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

    internal static double Luminosity(PrismBlendColor color)
    {
        return (0.3 * color.Red) +
            (0.59 * color.Green) +
            (0.11 * color.Blue);
    }

    internal static double Saturation(PrismBlendColor color)
    {
        return Math.Max(
                color.Red,
                Math.Max(color.Green, color.Blue)) -
            Math.Min(
                color.Red,
                Math.Min(color.Green, color.Blue));
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

}

internal readonly record struct PrismBlendColor(
    double Red,
    double Green,
    double Blue);

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
