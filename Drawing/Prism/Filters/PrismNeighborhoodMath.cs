using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismNeighborhoodMath
{
    internal static readonly Vector3 LuminanceWeights =
        new(0.2126f, 0.7152f, 0.0722f);

    private static readonly (int X, int Y)[] DespeckleKernel =
    [
        (0, 0),
        (-1, -1), (0, -1), (1, -1),
        (-1, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1),
        (0, -2), (2, 0), (0, 2), (-2, 0),
        (-1, -2), (1, -2),
        (2, -1), (2, 1),
        (1, 2), (-1, 2),
        (-2, 1), (-2, -1)
    ];

    public static PrismPremultipliedColor[] Apply(
        PrismNeighborhoodPlan plan,
        ReadOnlySpan<PrismPremultipliedColor> source,
        int width,
        int height,
        PrismColorProfile workingProfile,
        float opacity = 1,
        Func<Vector2, Vector4>? resource = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
        if (source.Length != checked(width * height))
        {
            throw new ArgumentException(
                "The source pixel count does not match its dimensions.",
                nameof(source));
        }
        if (!float.IsFinite(opacity) || opacity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }
        if (plan.Passes.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "A neighborhood plan must contain at least one pass.",
                nameof(plan));
        }
        if (plan.ResourceRequired && resource is null)
        {
            throw new InvalidOperationException(
                $"Filter '{plan.Filter}' requires its prepared resource.");
        }

        Vector4[] original = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            PrismPremultipliedColor linear =
                PrismAdjustmentMath.ConvertProfile(
                    source[index],
                    workingProfile,
                    PrismColorProfile.LinearSrgb);
            original[index] = ToVector4(linear);
        }

        if (plan.Operation == PrismNeighborhoodOperation.Despeckle)
        {
            Vector4[] despeckled = PrismDespeckleFilter.Apply(
                original,
                width,
                height,
                plan.Options0.X,
                plan.Options0.Y,
                (int)plan.Options0.Z);
            return CompleteResult(
                despeckled,
                original,
                workingProfile,
                opacity);
        }

        Vector4[] current = original;
        Vector4[] iterationEstimate = original;
        for (int passIndex = 0;
            passIndex < plan.Passes.Length;
            passIndex++)
        {
            PrismNeighborhoodPass pass = plan.Passes[passIndex];
            if (pass.IsNoOp)
            {
                continue;
            }

            if (plan.Operation == PrismNeighborhoodOperation.BoxBlur)
            {
                current = PrismBoxBlurFilter.ApplyPass(
                    current,
                    width,
                    height,
                    (int)pass.RadiusX,
                    (int)pass.RadiusY,
                    EdgeMode(plan));
                continue;
            }

            Vector4[] output = new Vector4[current.Length];
            if (plan.Operation ==
                    PrismNeighborhoodOperation.UnsharpMask &&
                pass.Kind == PrismNeighborhoodPassKind.Recombine)
            {
                for (int index = 0; index < output.Length; index++)
                {
                    output[index] = PrismUnsharpMaskFilter.Recombine(
                        original[index],
                        current[index],
                        plan.Options0.X,
                        plan.Options0.Z);
                }
                current = output;
                continue;
            }
            if (plan.Operation ==
                    PrismNeighborhoodOperation.HighPass &&
                pass.Kind == PrismNeighborhoodPassKind.Recombine)
            {
                for (int index = 0; index < output.Length; index++)
                {
                    output[index] = PrismHighPassFilter.Recombine(
                        original[index],
                        current[index]);
                }
                current = output;
                continue;
            }
            if (plan.Operation ==
                PrismNeighborhoodOperation.ReduceNoise)
            {
                if (pass.Kind is
                    PrismNeighborhoodPassKind.Horizontal or
                    PrismNeighborhoodPassKind.Vertical)
                {
                    current = PrismReduceNoiseFilter.ApplyDomainTransformPass(
                        plan,
                        pass,
                        current,
                        width,
                        height);
                    continue;
                }
                if (pass.Kind is
                    PrismNeighborhoodPassKind.JpegDeblockHorizontal or
                    PrismNeighborhoodPassKind.JpegDeblockVertical)
                {
                    current = PrismReduceNoiseFilter.ApplyJpegDeblockPass(
                        pass,
                        current,
                        width,
                        height);
                    continue;
                }
                if (pass.Kind == PrismNeighborhoodPassKind.Recombine)
                {
                    for (int index = 0; index < output.Length; index++)
                    {
                        output[index] = PrismReduceNoiseFilter.Recombine(
                            plan,
                            original[index],
                            current[index]);
                    }
                    current = output;
                    continue;
                }
            }
            if (plan.Operation ==
                PrismNeighborhoodOperation.SmartSharpen)
            {
                if (pass.Kind ==
                    PrismNeighborhoodPassKind.RichardsonLucyRatio)
                {
                    for (int index = 0; index < output.Length; index++)
                    {
                        output[index] = PrismSmartSharpenFilter.Ratio(
                            original[index],
                            current[index],
                            plan.Options0.Z);
                    }
                    current = output;
                    continue;
                }
                if (pass.Kind ==
                    PrismNeighborhoodPassKind.RichardsonLucyUpdate)
                {
                    for (int index = 0; index < output.Length; index++)
                    {
                        output[index] = PrismSmartSharpenFilter.Update(
                            iterationEstimate[index],
                            current[index]);
                    }
                    current = output;
                    iterationEstimate = current;
                    continue;
                }
                if (pass.Kind == PrismNeighborhoodPassKind.Recombine)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = (y * width) + x;
                            output[index] = PrismSmartSharpenFilter.Recombine(
                                plan,
                                original,
                                width,
                                height,
                                x,
                                y,
                                current[index]);
                        }
                    }
                    current = output;
                    continue;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[(y * width) + x] = ApplyPixel(
                        plan,
                        pass,
                        current,
                        width,
                        height,
                        x,
                        y,
                        resource);
                }
            }
            current = output;
        }

        return CompleteResult(
            current,
            original,
            workingProfile,
            opacity);
    }

    private static PrismPremultipliedColor[] CompleteResult(
        Vector4[] current,
        Vector4[] original,
        PrismColorProfile workingProfile,
        float opacity)
    {
        PrismPremultipliedColor[] result =
            new PrismPremultipliedColor[current.Length];
        for (int index = 0; index < current.Length; index++)
        {
            Vector4 filtered = ClampAssociated(current[index]);
            Vector4 blended = Vector4.Lerp(
                original[index],
                filtered,
                opacity);
            PrismPremultipliedColor linear =
                ToPremultiplied(blended);
            result[index] = PrismAdjustmentMath.ConvertProfile(
                linear,
                PrismColorProfile.LinearSrgb,
                workingProfile);
        }
        return result;
    }

    private static Vector4 ApplyPixel(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? resource)
    {
        Vector4 center = source[(y * width) + x];
        int edgeMode = EdgeMode(plan);
        return plan.Operation switch
        {
            PrismNeighborhoodOperation.Average =>
                PrismAverageFilter.Apply(source, width, height, x, y),
            PrismNeighborhoodOperation.Blur =>
                PrismBlurFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode),
            PrismNeighborhoodOperation.BlurMore =>
                PrismBlurMoreFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode),
            PrismNeighborhoodOperation.GaussianBlur =>
                PrismGaussianBlurFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode,
                    plan.Options0.W),
            PrismNeighborhoodOperation.LensBlur =>
                PrismLensBlurFilter.Apply(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    resource),
            PrismNeighborhoodOperation.MotionBlur =>
                PrismMotionBlurFilter.Apply(
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    edgeMode),
            PrismNeighborhoodOperation.ShapeBlur =>
                PrismShapeBlurFilter.Apply(
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    edgeMode,
                    resource!),
            PrismNeighborhoodOperation.SmartBlur =>
                PrismSmartBlurFilter.Apply(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y),
            PrismNeighborhoodOperation.SurfaceBlur =>
                PrismSurfaceBlurFilter.Apply(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y),
            PrismNeighborhoodOperation.Sharpen =>
                PrismSharpenFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    plan.Options0.X),
            PrismNeighborhoodOperation.SharpenMore =>
                PrismSharpenMoreFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    plan.Options0.X),
            PrismNeighborhoodOperation.SharpenEdges =>
                PrismSharpenEdgesFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    plan.Options0.X,
                    plan.Options0.Y),
            PrismNeighborhoodOperation.UnsharpMask =>
                PrismUnsharpMaskFilter.Sample(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode: 0,
                    plan.Options0.W),
            PrismNeighborhoodOperation.SmartSharpen =>
                PrismSmartSharpenFilter.Sample(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass.Kind ==
                        PrismNeighborhoodPassKind.RichardsonLucyBackProject),
            PrismNeighborhoodOperation.HighPass =>
                PrismHighPassFilter.Sample(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode,
                    plan.Options0.W),
            PrismNeighborhoodOperation.AddNoise =>
                PrismAddNoiseFilter.Apply(plan, center, x, y),
            PrismNeighborhoodOperation.Despeckle =>
                PrismDespeckleFilter.ApplyPixel(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    center),
            PrismNeighborhoodOperation.DustScratches =>
                PrismDustScratchesFilter.Apply(
                    source,
                    width,
                    height,
                    x,
                    y,
                    (int)plan.Options0.X,
                    plan.Options0.Y),
            PrismNeighborhoodOperation.Median =>
                PrismMedianFilter.Apply(source, width, height, x, y),
            PrismNeighborhoodOperation.ReduceNoise =>
                PrismReduceNoiseFilter.ApplyPixel(center),
            _ => SampleSpecialized(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y,
                center,
                resource)
        };
    }

    private static Vector4 SampleSpecialized(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector4 center,
        Func<Vector2, Vector4>? resource)
    {
        return plan.Operation switch
        {
            PrismNeighborhoodOperation.FieldBlur =>
                PrismFieldBlurFilter.Apply(
                    plan, pass, source, width, height, x, y, center, resource!),
            PrismNeighborhoodOperation.IrisBlur =>
                PrismIrisBlurFilter.Apply(
                    plan, pass, source, width, height, x, y, center),
            PrismNeighborhoodOperation.TiltShift =>
                PrismTiltShiftFilter.Apply(
                    plan, pass, source, width, height, x, y, center),
            PrismNeighborhoodOperation.PathBlur =>
                PrismPathBlurFilter.Apply(
                    plan, pass, source, width, height, x, y, resource),
            PrismNeighborhoodOperation.SpinBlur =>
                PrismSpinBlurFilter.Apply(
                    plan, pass, source, width, height, x, y),
            PrismNeighborhoodOperation.RadialBlur =>
                PrismRadialBlurFilter.Apply(
                    plan, pass, source, width, height, x, y),
            _ => throw new InvalidOperationException(
                $"Unsupported specialized neighborhood operation '{plan.Operation}'.")
        };
    }

    private static float SmoothStep(
        float start,
        float end,
        float value)
    {
        float amount = Math.Clamp(
            (value - start) / MathF.Max(end - start, 0.000001f),
            0,
            1);
        return amount * amount * (3 - (2 * amount));
    }

    internal static Vector4 Sample(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        int edgeMode)
    {
        int sampleX = (int)MathF.Round(x);
        int sampleY = (int)MathF.Round(y);
        if (edgeMode == 1 &&
            (sampleX < 0 ||
                sampleX >= width ||
                sampleY < 0 ||
                sampleY >= height))
        {
            return Vector4.Zero;
        }

        sampleX = edgeMode switch
        {
            2 => Wrap(sampleX, width),
            3 => Mirror(sampleX, width),
            _ => Math.Clamp(sampleX, 0, width - 1)
        };
        sampleY = edgeMode switch
        {
            2 => Wrap(sampleY, height),
            3 => Mirror(sampleY, height),
            _ => Math.Clamp(sampleY, 0, height - 1)
        };
        return source[(sampleY * width) + sampleX];
    }

    internal static Vector4 SampleBilinear(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        int edgeMode)
    {
        int left = (int)MathF.Floor(x);
        int top = (int)MathF.Floor(y);
        float fractionX = x - left;
        float fractionY = y - top;
        Vector4 topRow = Vector4.Lerp(
            Sample(source, width, height, left, top, edgeMode),
            Sample(source, width, height, left + 1, top, edgeMode),
            fractionX);
        Vector4 bottomRow = Vector4.Lerp(
            Sample(source, width, height, left, top + 1, edgeMode),
            Sample(source, width, height, left + 1, top + 1, edgeMode),
            fractionX);
        return Vector4.Lerp(topRow, bottomRow, fractionY);
    }

    internal static int EdgeMode(PrismNeighborhoodPlan plan) =>
        plan.Operation switch
        {
            PrismNeighborhoodOperation.Blur or
            PrismNeighborhoodOperation.BlurMore or
            PrismNeighborhoodOperation.BoxBlur or
            PrismNeighborhoodOperation.GaussianBlur or
            PrismNeighborhoodOperation.HighPass =>
                (int)plan.Options0.Z,
            PrismNeighborhoodOperation.MotionBlur =>
                (int)plan.Options0.W,
            PrismNeighborhoodOperation.ShapeBlur =>
                (int)plan.Options0.Y,
            PrismNeighborhoodOperation.SmartBlur or
            PrismNeighborhoodOperation.SurfaceBlur =>
                (int)plan.Options1.X,
            _ => 0
        };

    private static int Wrap(int value, int length)
    {
        int wrapped = value % length;
        return wrapped < 0 ? wrapped + length : wrapped;
    }

    private static int Mirror(int value, int length)
    {
        if (length == 1)
        {
            return 0;
        }
        int period = (length * 2) - 2;
        int mirrored = Wrap(value, period);
        return mirrored < length
            ? mirrored
            : period - mirrored;
    }

    private static Vector4 ClampAssociated(Vector4 color)
    {
        float alpha = Math.Clamp(color.W, 0, 1);
        return new Vector4(
            Math.Clamp(color.X, 0, alpha),
            Math.Clamp(color.Y, 0, alpha),
            Math.Clamp(color.Z, 0, alpha),
            alpha);
    }

    private static Vector3 Unpremultiply(Vector4 color) =>
        color.W <= 0
            ? Vector3.Zero
            : new Vector3(color.X, color.Y, color.Z) / color.W;

    internal static float Luminance(Vector4 color) =>
        Vector3.Dot(Unpremultiply(color), LuminanceWeights);

    private static Vector4 ToVector4(
        PrismPremultipliedColor color) =>
        new(
            (float)color.Red,
            (float)color.Green,
            (float)color.Blue,
            (float)color.Alpha);

    private static PrismPremultipliedColor ToPremultiplied(
        Vector4 color) =>
        new(color.X, color.Y, color.Z, color.W);
}
