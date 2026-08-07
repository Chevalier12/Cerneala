using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Styles;

internal enum PrismGradientInterpolation
{
    PerceptualOklab,
    ClassicSrgb
}

internal sealed class PrismCssGradientLut
{
    public const int SampleCount = 1_024;
    private readonly Vector4[] values;

    private PrismCssGradientLut(Vector4[] values) =>
        this.values = values;

    public ReadOnlySpan<Vector4> Values => values;

    public static PrismCssGradientLut Create(
        PrismGradientMapResource resource,
        PrismGradientInterpolation interpolation,
        PrismColorProfile workingProfile)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Vector4[] values = new Vector4[SampleCount];
        int segment = 0;
        for (int index = 0; index < values.Length; index++)
        {
            float coordinate = index / (SampleCount - 1f);
            while (segment + 1 < resource.Points.Length &&
                coordinate >= resource.Points[segment + 1].Offset)
            {
                segment++;
            }

            PrismGradientMapPoint left = resource.Points[segment];
            if (segment == resource.Points.Length - 1)
            {
                values[index] = ConvertPoint(left, workingProfile);
                continue;
            }

            PrismGradientMapPoint right = resource.Points[segment + 1];
            float amount = Math.Clamp(
                (coordinate - left.Offset) /
                    (right.Offset - left.Offset),
                0,
                1);
            values[index] = Interpolate(
                left,
                right,
                amount,
                interpolation,
                workingProfile);
        }
        return new PrismCssGradientLut(values);
    }

    public Vector4 Sample(float coordinate)
    {
        float scaled = Math.Clamp(coordinate, 0, 1) *
            (SampleCount - 1);
        int first = (int)MathF.Floor(scaled);
        int second = Math.Min(first + 1, SampleCount - 1);
        return Vector4.Lerp(
            values[first],
            values[second],
            scaled - first);
    }

    private static Vector4 Interpolate(
        PrismGradientMapPoint left,
        PrismGradientMapPoint right,
        float amount,
        PrismGradientInterpolation interpolation,
        PrismColorProfile workingProfile)
    {
        Vector3 leftColor = interpolation ==
            PrismGradientInterpolation.PerceptualOklab
                ? PrismOklab.FromLinearSrgb(left.LinearSrgb)
                : EncodeSrgb(left.LinearSrgb);
        Vector3 rightColor = interpolation ==
            PrismGradientInterpolation.PerceptualOklab
                ? PrismOklab.FromLinearSrgb(right.LinearSrgb)
                : EncodeSrgb(right.LinearSrgb);
        float alpha = left.Alpha +
            ((right.Alpha - left.Alpha) * amount);
        Vector3 associated = Vector3.Lerp(
            leftColor * left.Alpha,
            rightColor * right.Alpha,
            amount);
        if (alpha <= 0.000001f)
        {
            return Vector4.Zero;
        }

        Vector3 interpolated = associated / alpha;
        Vector3 linearSrgb = interpolation ==
            PrismGradientInterpolation.PerceptualOklab
                ? PrismOklab.ToLinearSrgb(interpolated)
                : DecodeSrgb(interpolated);
        Vector3 working = ConvertLinearSrgbToWorking(
            Vector3.Clamp(linearSrgb, Vector3.Zero, Vector3.One),
            workingProfile);
        return new Vector4(working * alpha, alpha);
    }

    private static Vector4 ConvertPoint(
        PrismGradientMapPoint point,
        PrismColorProfile workingProfile)
    {
        Vector3 working = ConvertLinearSrgbToWorking(
            point.LinearSrgb,
            workingProfile);
        return new Vector4(working * point.Alpha, point.Alpha);
    }

    private static Vector3 ConvertLinearSrgbToWorking(
        Vector3 value,
        PrismColorProfile profile)
    {
        PrismColorChannels linear = new(value.X, value.Y, value.Z);
        PrismColorChannels converted = profile switch
        {
            PrismColorProfile.Srgb =>
                PrismColorPipeline.EncodeSrgb(linear),
            PrismColorProfile.LinearDisplayP3 =>
                PrismColorPipeline.LinearSrgbToLinearDisplayP3(linear),
            PrismColorProfile.DisplayP3 =>
                PrismColorPipeline.EncodeSrgb(
                    PrismColorPipeline.LinearSrgbToLinearDisplayP3(linear)),
            PrismColorProfile.LinearSrgb or PrismColorProfile.ScRgb =>
                linear,
            _ => throw new ArgumentOutOfRangeException(
                nameof(profile),
                profile,
                "Unknown Prism gradient working profile.")
        };
        converted = PrismColorPipeline.Clamp01(converted);
        return new Vector3(
            (float)converted.Red,
            (float)converted.Green,
            (float)converted.Blue);
    }

    private static Vector3 EncodeSrgb(Vector3 value)
    {
        PrismColorChannels result = PrismColorPipeline.EncodeSrgb(
            new PrismColorChannels(value.X, value.Y, value.Z));
        return new Vector3(
            (float)result.Red,
            (float)result.Green,
            (float)result.Blue);
    }

    private static Vector3 DecodeSrgb(Vector3 value)
    {
        PrismColorChannels result = PrismColorPipeline.DecodeSrgb(
            new PrismColorChannels(value.X, value.Y, value.Z));
        return new Vector3(
            (float)result.Red,
            (float)result.Green,
            (float)result.Blue);
    }
}
