using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismNeighborhoodMath
{
    private static readonly Vector3 LuminanceWeights =
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
                    plan,
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

    internal static Vector4 SampleFieldBlur(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Vector2 uv,
        Vector4 center,
        Func<Vector2, Vector4> depthResource)
    {
        float depth = Math.Clamp(depthResource(uv).X, 0, 1);
        if (plan.Options0.Y > 0.5f)
        {
            depth = 1 - depth;
        }

        float focalDistance = Math.Clamp(plan.Options0.X, 0, 1);
        float range = MathF.Max(
            MathF.Max(focalDistance, 1 - focalDistance),
            0.000001f);
        float coc = Math.Clamp(
            MathF.Abs(depth - focalDistance) / range,
            0,
            1);
        if (coc <= 0.000001f)
        {
            return center;
        }

        int count = Math.Max(1, pass.SampleCount);
        float radiusX = pass.RadiusX * coc;
        float radiusY = pass.RadiusY * coc;
        float highlight = MathF.Max(0, plan.Options0.Z);
        Vector4 total = default;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            float fraction = count == 1 ? 0 : (float)index / (count - 1);
            float angle = index * 2.39996323f;
            Vector4 sample = Sample(
                source,
                width,
                height,
                x + (MathF.Cos(angle) * MathF.Sqrt(fraction) * radiusX),
                y + (MathF.Sin(angle) * MathF.Sqrt(fraction) * radiusY),
                EdgeMode(plan));
            float straightLuminance = sample.W > 0.000001f
                ? Vector3.Dot(new Vector3(sample.X, sample.Y, sample.Z) / sample.W, LuminanceWeights)
                : 0;
            float weight = 1 + (highlight * Math.Clamp(straightLuminance, 0, 1));
            total += sample * weight;
            totalWeight += weight;
        }

        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 SampleLens(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? resource)
    {
        Vector2 uv = new((x + 0.5f) / width, (y + 0.5f) / height);
        float depth = plan.Options1.W;
        if (resource is not null)
        {
            Vector4 map = resource(uv);
            depth = (int)plan.Options1.Z switch
            {
                1 => map.X,
                2 => map.Y,
                3 => map.Z,
                4 => map.W,
                _ => Vector3.Dot(new Vector3(map.X, map.Y, map.Z), LuminanceWeights)
            };
        }
        if (plan.Options2.X > 0.5f)
        {
            depth = 1 - depth;
        }

        float focus = resource is null
            ? 1
            : Math.Clamp(MathF.Abs(depth - plan.Options1.W), 0, 1);
        float radius = pass.RadiusX * focus;
        if (radius <= 0.000001f)
        {
            return source[(y * width) + x];
        }

        int count = Math.Max(1, pass.SampleCount);
        int blades = Math.Max(3, (int)MathF.Round(plan.Options0.Y));
        float curvature = Math.Clamp(plan.Options0.Z, 0, 1);
        float rotation = plan.Options0.W;
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            float fraction = count <= 1 ? 0 : (float)index / (count - 1);
            float angle = index * 2.39996323f;
            float sector = (2 * MathF.PI) / blades;
            float local = MathF.IEEERemainder(angle - rotation, sector);
            float polygonRadius = MathF.Cos(MathF.PI / blades) /
                MathF.Max(MathF.Cos(local), 0.000001f);
            float apertureRadius = polygonRadius +
                ((1 - polygonRadius) * curvature);
            float distance = MathF.Sqrt(fraction) * apertureRadius * radius;
            Vector4 sample = SampleBilinear(
                source,
                width,
                height,
                x + (MathF.Cos(angle) * distance),
                y + (MathF.Sin(angle) * distance),
                edgeMode: 0);
            float luminance = Vector3.Dot(Unpremultiply(sample), LuminanceWeights);
            float boost = luminance >= plan.Options1.Y
                ? MathF.Max(0, plan.Options1.X)
                : 0;
            sample = new Vector4(
                new Vector3(sample.X, sample.Y, sample.Z) * (1 + boost),
                sample.W);
            total += sample;
            totalWeight++;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 SampleMotion(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode)
    {
        return SampleLine(
            source,
            width,
            height,
            x,
            y,
            pass,
            edgeMode,
            gaussian: true,
            bilinear: true);
    }

    internal static Vector4 SampleShapePsf(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode,
        Func<Vector2, Vector4> resource)
    {
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int kernelY = 0; kernelY < count; kernelY++)
        {
            float v = (kernelY + 0.5f) / count;
            float offsetY = count == 1
                ? 0
                : (((float)kernelY / (count - 1)) * 2 - 1) * pass.RadiusY;
            for (int kernelX = 0; kernelX < count; kernelX++)
            {
                float u = (kernelX + 0.5f) / count;
                float offsetX = count == 1
                    ? 0
                    : (((float)kernelX / (count - 1)) * 2 - 1) * pass.RadiusX;
                float weight = MathF.Max(0, resource(new Vector2(u, v)).W);
                total += SampleBilinear(
                    source,
                    width,
                    height,
                    x - offsetX,
                    y - offsetY,
                    edgeMode) * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0.000001f
            ? total / totalWeight
            : source[(y * width) + x];
    }

    internal static Vector4 SamplePath(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? resource)
    {
        int count = Math.Max(1, pass.SampleCount);
        int intervalCount = count - 1;
        Vector2 origin = new(x, y);
        Vector4 center = source[(y * width) + x];
        Vector4 centerField = resource!(
            PathUv(origin, width, height));
        float centerWeight = Math.Clamp(centerField.W, 0, 1);
        if (intervalCount == 0 || centerWeight <= 0.000001f)
        {
            return center;
        }

        bool centered = plan.Options0.Z > 0.5f;
        int flashSync = centered ? 1 : (int)plan.Options1.Y;
        int backwardSteps = flashSync switch
        {
            0 => intervalCount,
            1 => intervalCount / 2,
            _ => 0
        };
        int forwardSteps = intervalCount - backwardSteps;
        Vector4 total = center * centerWeight;
        float totalWeight = centerWeight;
        AccumulatePathDirection(
            plan,
            source,
            width,
            height,
            x,
            y,
            resource,
            origin,
            centerField,
            backwardSteps,
            intervalCount,
            directionSign: -1,
            ref total,
            ref totalWeight);
        AccumulatePathDirection(
            plan,
            source,
            width,
            height,
            x,
            y,
            resource,
            origin,
            centerField,
            forwardSteps,
            intervalCount,
            directionSign: 1,
            ref total,
            ref totalWeight);
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    private static void AccumulatePathDirection(
        PrismNeighborhoodPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4> resource,
        Vector2 origin,
        Vector4 originField,
        int stepCount,
        int intervalCount,
        int directionSign,
        ref Vector4 total,
        ref float totalWeight)
    {
        Vector2 position = origin;
        for (int step = 1; step <= stepCount; step++)
        {
            if (!TryPathRk4Step(
                plan,
                resource,
                width,
                height,
                position,
                step == 1 ? originField : null,
                intervalCount,
                directionSign,
                out Vector2 next,
                out Vector2 tangent,
                out float validity,
                out float stepLength))
            {
                break;
            }

            position = next;
            float jitter = PathNoise(
                x,
                y,
                step,
                directionSign) *
                Math.Clamp(MathF.Abs(plan.Options1.Z), 0, 1) *
                stepLength *
                0.5f;
            Vector2 samplePosition = position + (tangent * jitter);
            float profileWeight = PathProfileWeight(
                plan,
                (float)step / Math.Max(stepCount, 1));
            float weight = validity * profileWeight;
            total += SampleBilinear(
                source,
                width,
                height,
                samplePosition.X,
                samplePosition.Y,
                edgeMode: 0) * weight;
            totalWeight += weight;
        }
    }

    private static bool TryPathRk4Step(
        PrismNeighborhoodPlan plan,
        Func<Vector2, Vector4> resource,
        int width,
        int height,
        Vector2 position,
        Vector4? initialField,
        int intervalCount,
        int directionSign,
        out Vector2 next,
        out Vector2 tangent,
        out float validity,
        out float stepLength)
    {
        next = position;
        tangent = Vector2.Zero;
        validity = 0;
        stepLength = 0;
        bool firstValid = initialField.HasValue
            ? TryPathDerivative(
                plan,
                initialField.Value,
                intervalCount,
                directionSign,
                out Vector2 k1,
                out _,
                out _)
            : TryPathDerivative(
                plan,
                resource,
                width,
                height,
                position,
                intervalCount,
                directionSign,
                out k1,
                out _,
                out _);
        if (!firstValid)
        {
            return false;
        }
        if (!TryPathDerivative(
            plan,
            resource,
            width,
            height,
            position + (k1 * 0.5f),
            intervalCount,
            directionSign,
            out Vector2 k2,
            out _,
            out _))
        {
            return false;
        }
        if (!TryPathDerivative(
            plan,
            resource,
            width,
            height,
            position + (k2 * 0.5f),
            intervalCount,
            directionSign,
            out Vector2 k3,
            out _,
            out _))
        {
            return false;
        }
        if (!TryPathDerivative(
            plan,
            resource,
            width,
            height,
            position + k3,
            intervalCount,
            directionSign,
            out Vector2 k4,
            out tangent,
            out validity))
        {
            return false;
        }

        Vector2 displacement =
            (k1 + (k2 * 2) + (k3 * 2) + k4) / 6;
        next = position + displacement;
        stepLength = displacement.Length();
        return float.IsFinite(next.X) &&
            float.IsFinite(next.Y) &&
            float.IsFinite(stepLength);
    }

    private static bool TryPathDerivative(
        PrismNeighborhoodPlan plan,
        Func<Vector2, Vector4> resource,
        int width,
        int height,
        Vector2 position,
        int intervalCount,
        int directionSign,
        out Vector2 derivative,
        out Vector2 tangent,
        out float validity)
    {
        Vector4 field = resource(PathUv(position, width, height));
        return TryPathDerivative(
            plan,
            field,
            intervalCount,
            directionSign,
            out derivative,
            out tangent,
            out validity);
    }

    private static bool TryPathDerivative(
        PrismNeighborhoodPlan plan,
        Vector4 field,
        int intervalCount,
        int directionSign,
        out Vector2 derivative,
        out Vector2 tangent,
        out float validity)
    {
        validity = Math.Clamp(field.W, 0, 1);
        Vector2 direction = new(
            (field.X * 2) - 1,
            (field.Y * 2) - 1);
        float directionLengthSquared = direction.LengthSquared();
        if (validity <= 0.000001f ||
            directionLengthSquared <= 0.000001f ||
            !float.IsFinite(directionLengthSquared))
        {
            derivative = Vector2.Zero;
            tangent = Vector2.Zero;
            return false;
        }

        direction /= MathF.Sqrt(directionLengthSquared);
        float speed = plan.Options0.X +
            ((plan.Options0.W - plan.Options0.X) *
                Math.Clamp(field.Z, 0, 1));
        derivative =
            direction *
            (speed / Math.Max(intervalCount, 1)) *
            directionSign;
        float derivativeLength = derivative.Length();
        tangent = derivativeLength > 0.000001f
            ? derivative / derivativeLength
            : direction * directionSign;
        return float.IsFinite(derivative.X) &&
            float.IsFinite(derivative.Y);
    }

    private static float PathProfileWeight(
        PrismNeighborhoodPlan plan,
        float distanceFraction)
    {
        if ((int)plan.Options1.X == 0)
        {
            return 1;
        }

        float taper = Math.Clamp(plan.Options0.Y, 0, 1);
        return 1 - (taper * Math.Clamp(distanceFraction, 0, 1));
    }

    private static Vector2 PathUv(
        Vector2 pixelPosition,
        int width,
        int height) =>
        new(
            (pixelPosition.X + 0.5f) / width,
            (pixelPosition.Y + 0.5f) / height);

    private static float PathNoise(
        int x,
        int y,
        int step,
        int directionSign)
    {
        float hashX = x + (step * 19);
        float hashY = y + (directionSign < 0 ? 37 : 73);
        float value =
            (hashX * 127.1f) +
            (hashY * 311.7f);
        float unbounded = MathF.Sin(
            value + (2791 * 0.00006103515625f)) *
            43758.5453123f;
        float fraction = unbounded - MathF.Floor(unbounded);
        return (fraction * 2) - 1;
    }

    internal static Vector4 SampleRadial(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector2 center = plan.Operation == PrismNeighborhoodOperation.SpinBlur
            ? new Vector2(plan.Options0.X, plan.Options0.Y)
            : new Vector2(plan.Options0.Z, plan.Options0.W);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector2 delta = uv - center;
        float amount = plan.Operation == PrismNeighborhoodOperation.SpinBlur
            ? plan.Options1.X
            : plan.Options0.Y;
        bool zoom = plan.Operation == PrismNeighborhoodOperation.RadialBlur &&
            plan.Options0.X > 0.5f;
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        for (int index = 0; index < count; index++)
        {
            float position = count <= 1
                ? 0
                : ((float)index / (count - 1)) - 0.5f;
            Vector2 sampleUv;
            if (zoom)
            {
                sampleUv = Vector2.Lerp(
                    uv,
                    center,
                    position * amount);
            }
            else
            {


                Vector2 pixelDelta = new(
                    delta.X * width,
                    delta.Y * height);
                float angle = position * amount;
                float cosine = MathF.Cos(angle);
                float sine = MathF.Sin(angle);
                sampleUv = center + new Vector2(
                    ((pixelDelta.X * cosine) -
                        (pixelDelta.Y * sine)) / width,
                    ((pixelDelta.X * sine) +
                        (pixelDelta.Y * cosine)) / height);
            }
            total += SampleBilinear(
                source,
                width,
                height,
                (sampleUv.X * width) - 0.5f,
                (sampleUv.Y * height) - 0.5f,
                edgeMode: 0);
        }
        return total / count;
    }

    internal static Vector4 SampleSpin(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 centerSample = source[(y * width) + x];
        Vector2 center = new(plan.Options0.X, plan.Options0.Y);
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector2 delta = uv - center;
        Vector2 radius = new(plan.Options0.Z, plan.Options0.W);
        Vector2 normalized = new(
            delta.X / MathF.Max(radius.X, 0.000001f),
            delta.Y / MathF.Max(radius.Y, 0.000001f));
        float distance = normalized.Length();
        float feather = Math.Clamp(plan.Options1.Y, 0, 1);
        float mask;
        if (feather <= 0.000001f)
        {
            mask = distance <= 1 ? 1 : 0;
        }
        else
        {
            float transition = Math.Clamp(
                (1 - distance) / feather,
                0,
                1);
            mask =
                transition *
                transition *
                (3 - (2 * transition));
        }
        if (mask <= 0.000001f)
        {
            return centerSample;
        }

        Vector2 pixelDelta = new(
            delta.X * width,
            delta.Y * height);
        float rotation = plan.Options1.X;
        int count = PrismNeighborhoodPlanner.SpinSampleCount(
            MathF.Abs(rotation) * pixelDelta.Length(),
            pass.SampleCount);
        if (count <= 1)
        {
            return centerSample;
        }

        int intervals = count - 1;
        float angleStep = rotation / intervals;
        float noise = Math.Clamp(plan.Options2.Y, 0, 1);
        float strobeStrength = Math.Clamp(plan.Options1.Z, 0, 1);
        int strobeFlashes = Math.Max(
            0,
            (int)MathF.Round(plan.Options1.W));
        float strobeDuration = Math.Clamp(plan.Options2.X, 0, 1);
        Vector2 rotatedDelta = Rotate(
            pixelDelta,
            rotation * -0.5f);
        float stepCosine = MathF.Cos(angleStep);
        float stepSine = MathF.Sin(angleStep);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            Vector2 sampleDelta = rotatedDelta;
            if (noise > 0 &&
                index > 0 &&
                index < intervals &&
                index != intervals / 2)
            {
                float jitter =
                    Noise(x, y, 0x51f15e5du, (uint)index) *
                    noise *
                    angleStep *
                    0.5f;
                sampleDelta = Rotate(sampleDelta, jitter);
            }

            float position = (float)index / intervals;
            float weight = SpinStrobeWeight(
                position,
                strobeStrength,
                strobeFlashes,
                strobeDuration);
            Vector2 sampleUv = center + new Vector2(
                sampleDelta.X / width,
                sampleDelta.Y / height);
            total += SampleBilinear(
                source,
                width,
                height,
                (sampleUv.X * width) - 0.5f,
                (sampleUv.Y * height) - 0.5f,
                edgeMode: 0) * weight;
            totalWeight += weight;
            rotatedDelta = new Vector2(
                (rotatedDelta.X * stepCosine) -
                    (rotatedDelta.Y * stepSine),
                (rotatedDelta.X * stepSine) +
                    (rotatedDelta.Y * stepCosine));
        }

        Vector4 blurred = totalWeight > 0.000001f
            ? total / totalWeight
            : centerSample;
        return Vector4.Lerp(centerSample, blurred, mask);
    }

    private static Vector2 Rotate(
        Vector2 value,
        float angle)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(
            (value.X * cosine) - (value.Y * sine),
            (value.X * sine) + (value.Y * cosine));
    }

    private static float SpinStrobeWeight(
        float position,
        float strength,
        int flashes,
        float duration)
    {
        if (strength <= 0 || flashes <= 0)
        {
            return 1;
        }

        float phase = (position * flashes) + 0.5f;
        phase -= MathF.Floor(phase);
        float pulse = MathF.Abs(phase - 0.5f) <= duration * 0.5f
            ? 1
            : 0;
        return 1 + ((pulse - 1) * strength);
    }

    internal static Vector4[] ApplyBoxBlurSat(
        Vector4[] source,
        int width,
        int height,
        int radiusX,
        int radiusY,
        int edgeMode)
    {
        int paddedWidth = checked(width + (radiusX * 2));
        int paddedHeight = checked(height + (radiusY * 2));
        int stride = checked(paddedWidth + 1);
        Vector4[] sat = new Vector4[checked(stride * (paddedHeight + 1))];
        for (int y = 0; y < paddedHeight; y++)
        {
            Vector4 row = Vector4.Zero;
            for (int x = 0; x < paddedWidth; x++)
            {
                row += Sample(
                    source,
                    width,
                    height,
                    x - radiusX,
                    y - radiusY,
                    edgeMode);
                sat[((y + 1) * stride) + x + 1] =
                    sat[(y * stride) + x + 1] + row;
            }
        }

        int diameterX = checked((radiusX * 2) + 1);
        int diameterY = checked((radiusY * 2) + 1);
        float normalization = 1f / checked(diameterX * diameterY);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int right = x + diameterX;
                int bottom = y + diameterY;
                Vector4 sum =
                    sat[(bottom * stride) + right] -
                    sat[(y * stride) + right] -
                    sat[(bottom * stride) + x] +
                    sat[(y * stride) + x];
                result[(y * width) + x] = sum * normalization;
            }
        }
        return result;
    }

    private static Vector4 SampleLine(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode,
        bool gaussian,
        bool bilinear)
    {
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int index = 0; index < count; index++)
        {
            float position = count <= 1
                ? 0
                : (((float)index / (count - 1)) * 2) - 1;
            float weight = gaussian
                ? MathF.Exp(-3.125f * position * position)
                : 1;
            float sampleX = x + (pass.RadiusX * position);
            float sampleY = y + (pass.RadiusY * position);
            total += (bilinear
                ? SampleBilinear(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY,
                    edgeMode)
                : Sample(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY,
                    edgeMode)) * weight;
            totalWeight += weight;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 SampleIncrementalGaussian(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode,
        float sigma)
    {
        int halfTapCount = Math.Max(0, (pass.SampleCount - 1) / 2);
        if (halfTapCount == 0 || sigma <= 0)
        {
            return source[(y * width) + x];
        }

        float stepX = pass.RadiusX / halfTapCount;
        float stepY = pass.RadiusY / halfTapCount;
        float stepSquared = (stepX * stepX) + (stepY * stepY);
        float ratio = MathF.Exp(-stepSquared / (2 * sigma * sigma));
        float ratioStep = ratio * ratio;
        float weight = 1;
        float multiplier = ratio;
        Vector4 total = source[(y * width) + x];
        float totalWeight = 1;
        for (int tap = 1; tap <= halfTapCount; tap++)
        {
            weight *= multiplier;
            multiplier *= ratioStep;
            float offsetX = tap * stepX;
            float offsetY = tap * stepY;
            total += (Sample(
                source, width, height, x + offsetX, y + offsetY, edgeMode) +
                Sample(
                    source, width, height, x - offsetX, y - offsetY, edgeMode)) *
                weight;
            totalWeight += weight * 2;
        }
        return total / totalWeight;
    }

    internal static Vector4 SampleDisk(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode)
    {
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Sample(
            source,
            width,
            height,
            x,
            y,
            edgeMode);
        for (int index = 1; index < count; index++)
        {
            float fraction = (float)index / Math.Max(count - 1, 1);
            float angle = index * 2.39996323f;
            total += Sample(
                source,
                width,
                height,
                x + (
                    MathF.Cos(angle) *
                    MathF.Sqrt(fraction) *
                    pass.RadiusX),
                y + (
                    MathF.Sin(angle) *
                    MathF.Sqrt(fraction) *
                    pass.RadiusY),
                edgeMode);
        }
        return total / count;
    }

    internal static Vector4 SampleOptimizedBilinearGaussian(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode)
    {
        int halfTapCount = Math.Max(0, (pass.SampleCount - 1) / 2);
        if (halfTapCount == 0)
        {
            return source[(y * width) + x];
        }

        float centerWeight = 1;
        Vector4 total = source[(y * width) + x] * centerWeight;
        float totalWeight = centerWeight;
        float stepX = pass.RadiusX / halfTapCount;
        float stepY = pass.RadiusY / halfTapCount;
        for (int firstTap = 1;
            firstTap <= halfTapCount;
            firstTap += 2)
        {
            int secondTap = firstTap + 1;
            float firstPosition = (float)firstTap / halfTapCount;
            float firstWeight = MathF.Exp(
                -3.125f * firstPosition * firstPosition);
            float secondWeight = 0;
            if (secondTap <= halfTapCount)
            {
                float secondPosition =
                    (float)secondTap / halfTapCount;
                secondWeight = MathF.Exp(
                    -3.125f * secondPosition * secondPosition);
            }

            float pairWeight = firstWeight + secondWeight;
            float pairOffset = firstTap +
                (secondWeight / MathF.Max(pairWeight, 0.000001f));
            float offsetX = pairOffset * stepX;
            float offsetY = pairOffset * stepY;
            total += (
                SampleBilinear(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode) +
                SampleBilinear(
                    source,
                    width,
                    height,
                    x - offsetX,
                    y - offsetY,
                    edgeMode)) * pairWeight;
            totalWeight += pairWeight * 2;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 SampleSmartBlur(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = source[(y * width) + x];
        Vector3 centerStraight = Unpremultiply(center);
        float radius = plan.Options0.X;
        float rangeSigma = plan.Options0.Y;
        float spatialSigma = MathF.Max(radius / 3f, 0.000001f);
        float inverseSpatialVariance = 0.5f / (spatialSigma * spatialSigma);
        float inverseRangeVariance = rangeSigma > 0
            ? 0.5f / (rangeSigma * rangeSigma)
            : 0;
        int diameter = Math.Max(1, pass.SampleCount);
        int half = diameter / 2;
        float step = half > 0 ? radius / half : 0;
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int offsetY = -half; offsetY <= half; offsetY++)
        {
            for (int offsetX = -half; offsetX <= half; offsetX++)
            {
                float sampleX = offsetX * step;
                float sampleY = offsetY * step;
                float distanceSquared =
                    (sampleX * sampleX) + (sampleY * sampleY);
                if (distanceSquared > radius * radius)
                {
                    continue;
                }
                Vector4 sample = Sample(
                    source,
                    width,
                    height,
                    x + sampleX,
                    y + sampleY,
                    (int)plan.Options1.X);
                Vector3 colorDelta = Unpremultiply(sample) - centerStraight;
                float rangeDistanceSquared = colorDelta.LengthSquared() / 3f;
                float rangeWeight = rangeSigma > 0
                    ? MathF.Exp(-rangeDistanceSquared * inverseRangeVariance)
                    : rangeDistanceSquared <= 0.0000001f ? 1 : 0;
                float spatialWeight = MathF.Exp(
                    -distanceSquared * inverseSpatialVariance);
                float weight = spatialWeight * rangeWeight;
                total += sample * weight;
                totalWeight += weight;
            }
        }

        Vector4 blurred = total / MathF.Max(totalWeight, 0.000001f);
        int mode = (int)plan.Options0.W;
        if (mode == 0)
        {
            return blurred;
        }

        float edge = Math.Clamp(
            Vector3.Distance(centerStraight, Unpremultiply(blurred)),
            0,
            1);
        Vector4 edgeColor = new(new Vector3(edge * center.W), center.W);
        return mode == 1
            ? edgeColor
            : Vector4.Lerp(center, edgeColor, edge);
    }

    internal static Vector4 SampleSurfaceBilateral(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = source[(y * width) + x];
        float centerLuminance = Vector3.Dot(
            Unpremultiply(center),
            LuminanceWeights);
        float radius = plan.Options0.X;
        float rangeSigma = plan.Options0.Y;
        float spatialSigma = MathF.Max(radius / 3f, 0.000001f);
        float inverseSpatialVariance = 0.5f / (spatialSigma * spatialSigma);
        float inverseRangeVariance = rangeSigma > 0
            ? 0.5f / (rangeSigma * rangeSigma)
            : 0;
        int diameter = Math.Max(1, pass.SampleCount);
        int half = diameter / 2;
        float step = half > 0 ? radius / half : 0;
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int offsetY = -half; offsetY <= half; offsetY++)
        {
            for (int offsetX = -half; offsetX <= half; offsetX++)
            {
                float sampleX = offsetX * step;
                float sampleY = offsetY * step;
                float distanceSquared =
                    (sampleX * sampleX) + (sampleY * sampleY);
                if (distanceSquared > radius * radius)
                {
                    continue;
                }
                Vector4 sample = Sample(
                    source,
                    width,
                    height,
                    x + sampleX,
                    y + sampleY,
                    (int)plan.Options1.X);
                float rangeDistance = Vector3.Dot(
                    Unpremultiply(sample),
                    LuminanceWeights) - centerLuminance;
                float rangeDistanceSquared = rangeDistance * rangeDistance;
                float rangeWeight = rangeSigma > 0
                    ? MathF.Exp(-rangeDistanceSquared * inverseRangeVariance)
                    : rangeDistanceSquared <= 0.0000001f ? 1 : 0;
                float spatialWeight = MathF.Exp(
                    -distanceSquared * inverseSpatialVariance);
                float weight = spatialWeight * rangeWeight;
                total += sample * weight;
                totalWeight += weight;
            }
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    internal static Vector4 Neighborhood3x3(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 total = Vector4.Zero;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                total += Sample(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode: 0);
            }
        }
        return total / 9;
    }

    internal static Vector4 Median3x3(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Span<Vector4> values = stackalloc Vector4[9];
        Span<float> ranks = stackalloc float[9];
        int index = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                Vector4 sample = Sample(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode: 0);
                values[index] = sample;
                ranks[index] = Luminance(sample);
                index++;
            }
        }
        MedianSortNetwork(values, ranks);
        return values[4];
    }

    private static void MedianSortNetwork(
        Span<Vector4> values,
        Span<float> ranks)
    {
        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);

        CompareExchange(ref values[1], ref ranks[1], ref values[2], ref ranks[2]);
        CompareExchange(ref values[3], ref ranks[3], ref values[4], ref ranks[4]);
        CompareExchange(ref values[5], ref ranks[5], ref values[6], ref ranks[6]);
        CompareExchange(ref values[7], ref ranks[7], ref values[8], ref ranks[8]);

        CompareExchange(ref values[0], ref ranks[0], ref values[1], ref ranks[1]);
        CompareExchange(ref values[2], ref ranks[2], ref values[3], ref ranks[3]);
        CompareExchange(ref values[4], ref ranks[4], ref values[5], ref ranks[5]);
        CompareExchange(ref values[6], ref ranks[6], ref values[7], ref ranks[7]);
    }

    private static void CompareExchange(
        ref Vector4 left,
        ref float leftRank,
        ref Vector4 right,
        ref float rightRank)
    {
        if (rightRank >= leftRank)
        {
            return;
        }

        (left, right) = (right, left);
        (leftRank, rightRank) = (rightRank, leftRank);
    }

    internal static Vector4 AdaptiveThresholdedMedian(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int maximumRadius,
        float threshold)
    {
        Vector4 center = source[(y * width) + x];
        float centerLuminance = LuminanceStraight(center);
        Vector4 fallback = center;
        Span<Vector4> values = stackalloc Vector4[49];
        int boundedRadius = Math.Clamp(maximumRadius, 1, 3);
        for (int radius = 1; radius <= boundedRadius; radius++)
        {
            int count = 0;
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    values[count++] = Sample(
                        source,
                        width,
                        height,
                        x + offsetX,
                        y + offsetY,
                        edgeMode: 0);
                }
            }

            Span<Vector4> window = values[..count];
            window.Sort(
                static (left, right) =>
                    LuminanceStraight(left).CompareTo(
                        LuminanceStraight(right)));
            Vector4 median = window[count / 2];
            fallback = median;
            float minimum = LuminanceStraight(window[0]);
            float medianLuminance = LuminanceStraight(median);
            float maximum = LuminanceStraight(window[^1]);
            if (medianLuminance <= minimum ||
                medianLuminance >= maximum)
            {
                continue;
            }

            bool centerIsImpulse =
                centerLuminance <= minimum ||
                centerLuminance >= maximum;
            return centerIsImpulse &&
                MathF.Abs(centerLuminance - medianLuminance) > threshold
                    ? PreserveCoverage(center, median)
                    : center;
        }

        return MathF.Abs(
            centerLuminance - LuminanceStraight(fallback)) > threshold
                ? PreserveCoverage(center, fallback)
                : center;
    }

    internal static Vector4[] ApplyProgressiveDespeckle(
        Vector4[] original,
        int width,
        int height,
        float threshold,
        float radius,
        int iterationCount)
    {
        if (radius <= 0 || iterationCount <= 0)
        {
            return (Vector4[])original.Clone();
        }

        bool[] impulses = new bool[original.Length];
        Vector4[] detection = (Vector4[])original.Clone();
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            Vector4[] output = new Vector4[detection.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;
                    Vector4 center = detection[index];
                    Vector4 median = DespeckleMedian(
                        detection,
                        impulses: null,
                        goodOnly: false,
                        width,
                        height,
                        x,
                        y,
                        radius,
                        out _);
                    bool detected = MathF.Abs(
                        LuminanceStraight(center) -
                        LuminanceStraight(median)) > threshold;
                    impulses[index] |= detected;
                    output[index] = detected
                        ? PreserveCoverage(center, median)
                        : center;
                }
            }
            detection = output;
        }

        Vector4[] current = (Vector4[])original.Clone();
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            Vector4[] output = (Vector4[])current.Clone();
            bool[] remaining = (bool[])impulses.Clone();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;
                    if (!impulses[index])
                    {
                        continue;
                    }

                    Vector4 median = DespeckleMedian(
                        current,
                        impulses,
                        goodOnly: true,
                        width,
                        height,
                        x,
                        y,
                        radius,
                        out bool found);
                    if (!found)
                    {
                        continue;
                    }

                    output[index] = PreserveCoverage(
                        original[index],
                        median);
                    remaining[index] = false;
                }
            }
            current = output;
            impulses = remaining;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                if (!impulses[index])
                {
                    continue;
                }

                Vector4 median = DespeckleMedian(
                    current,
                    impulses: null,
                    goodOnly: false,
                    width,
                    height,
                    x,
                    y,
                    radius,
                    out _);
                current[index] = PreserveCoverage(
                    original[index],
                    median);
            }
        }

        return current;
    }

    private static Vector4 DespeckleMedian(
        Vector4[] source,
        bool[]? impulses,
        bool goodOnly,
        int width,
        int height,
        int x,
        int y,
        float radius,
        out bool found)
    {
        Span<Vector4> samples =
            stackalloc Vector4[21];
        int count = 0;
        int kernelCount = radius <= 1.5f ? 9 : DespeckleKernel.Length;
        float scale = radius <= 1.5f ? 1 : radius / 2;
        for (int sampleIndex = 0;
            sampleIndex < kernelCount;
            sampleIndex++)
        {
            (int kernelX, int kernelY) =
                DespeckleKernel[sampleIndex];
            int offsetX = (int)MathF.Round(
                kernelX * scale,
                MidpointRounding.AwayFromZero);
            int offsetY = (int)MathF.Round(
                kernelY * scale,
                MidpointRounding.AwayFromZero);
            int sampleX = Math.Clamp(x + offsetX, 0, width - 1);
            int sampleY = Math.Clamp(y + offsetY, 0, height - 1);
            int sourceIndex = (sampleY * width) + sampleX;
            if (goodOnly && impulses![sourceIndex])
            {
                continue;
            }
            samples[count++] = source[sourceIndex];
        }

        found = count > 0;
        if (!found)
        {
            return source[(y * width) + x];
        }
        samples[..count].Sort(
            static (left, right) =>
                LuminanceStraight(left).CompareTo(
                    LuminanceStraight(right)));
        return samples[(count - 1) / 2];
    }

    private static float LuminanceStraight(Vector4 color) =>
        Vector3.Dot(Unpremultiply(color), LuminanceWeights);

    private static Vector4 PreserveCoverage(
        Vector4 center,
        Vector4 replacement)
    {
        float alpha = Math.Clamp(center.W, 0, 1);
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(replacement),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * alpha, alpha);
    }

    internal static Vector4 AddNoise(
        PrismNeighborhoodPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        uint seed =
            ((uint)plan.Options1.X << 16) |
            (uint)plan.Options0.W;
        bool gaussian = plan.Options0.Y > 0.5f;
        float red = AddNoiseSample(x, y, seed, 0, gaussian);
        float green = plan.Options0.Z > 0.5f
            ? red
            : AddNoiseSample(x, y, seed, 1, gaussian);
        float blue = plan.Options0.Z > 0.5f
            ? red
            : AddNoiseSample(x, y, seed, 2, gaussian);
        Vector3 noise = new(red, green, blue);
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(center) +
                (noise * plan.Options0.X),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * center.W,
            center.W);
    }

    internal static Vector4 SampleRichardsonLucyPsf(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        bool correction)
    {
        Vector4 center = source[(y * width) + x];
        Vector3 fallback = correction
            ? Vector3.One
            : SharpenStraight(center, Vector3.Zero);
        Vector3 total = Vector3.Zero;
        float totalWeight = 0;
        int count = Math.Clamp(pass.SampleCount, 1, 17);
        float radius = MathF.Max(0, plan.Options0.Y);
        int remove = (int)plan.Options0.W;
        if (remove == 2)
        {
            float angle = plan.Options1.X;
            Vector2 direction = new(
                MathF.Cos(angle),
                -MathF.Sin(angle));
            for (int index = 0; index < count; index++)
            {
                float position = count <= 1
                    ? 0
                    : ((index / (count - 1f)) * 2) - 1;
                Accumulate(position * radius * direction, 1);
            }
        }
        else
        {
            int half = count / 2;
            float step = half == 0 ? 0 : radius / half;
            float sigma = MathF.Max(radius / 3f, 0.000001f);
            float inverseVariance = 0.5f / (sigma * sigma);
            for (int sampleY = -half; sampleY <= half; sampleY++)
            {
                for (int sampleX = -half; sampleX <= half; sampleX++)
                {
                    Vector2 offset = new(
                        sampleX * step,
                        sampleY * step);
                    float distanceSquared = offset.LengthSquared();
                    if (remove == 1 &&
                        distanceSquared > radius * radius)
                    {
                        continue;
                    }
                    float weight = remove == 0
                        ? MathF.Exp(-distanceSquared * inverseVariance)
                        : 1;
                    Accumulate(offset, weight);
                }
            }
        }

        Vector3 result = totalWeight > 0.000001f
            ? total / totalWeight
            : fallback;
        if (correction)
        {
            return new Vector4(result, 1);
        }
        return new Vector4(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One) * center.W,
            center.W);

        void Accumulate(Vector2 offset, float weight)
        {
            Vector4 sample = SampleBilinear(
                source,
                width,
                height,
                x + offset.X,
                y + offset.Y,
                edgeMode: 0);
            Vector3 straight = correction
                ? new Vector3(sample.X, sample.Y, sample.Z)
                : SharpenStraight(sample, fallback);
            total += straight * weight;
            totalWeight += weight;
        }
    }

    internal static Vector4 RichardsonLucyRatio(
        Vector4 original,
        Vector4 blurred,
        float reduceNoise)
    {
        Vector3 observed = SharpenStraight(original, Vector3.Zero);
        Vector3 estimate = SharpenStraight(blurred, observed);
        Vector3 ratio = new(
            observed.X / MathF.Max(estimate.X, 1f / 4096),
            observed.Y / MathF.Max(estimate.Y, 1f / 4096),
            observed.Z / MathF.Max(estimate.Z, 1f / 4096));
        ratio = Vector3.Clamp(ratio, Vector3.Zero, new Vector3(16));
        float updateStrength =
            1 - Math.Clamp(reduceNoise, 0, 1);
        return new Vector4(
            Vector3.Lerp(Vector3.One, ratio, updateStrength),
            1);
    }

    internal static Vector4 RichardsonLucyUpdate(
        Vector4 estimate,
        Vector4 correction)
    {
        Vector3 straight = SharpenStraight(estimate, Vector3.Zero);
        Vector3 factor = Vector3.Clamp(
            new Vector3(correction.X, correction.Y, correction.Z),
            Vector3.Zero,
            new Vector3(16));
        straight = Vector3.Clamp(
            straight * factor,
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * estimate.W, estimate.W);
    }

    internal static Vector4 SmartSharpenRecombine(
        PrismNeighborhoodPlan plan,
        Vector4[] original,
        int width,
        int height,
        int x,
        int y,
        Vector4 restored)
    {
        int index = (y * width) + x;
        Vector4 source = original[index];
        if (source.W <= 0.000001f)
        {
            return source;
        }

        Vector3 sourceStraight = SharpenStraight(source, Vector3.Zero);
        Vector3 restoredStraight =
            SharpenStraight(restored, sourceStraight);
        float shadowLuminance = LocalLuminance(
            original,
            width,
            height,
            x,
            y,
            plan.Options1.W);
        float highlightLuminance = LocalLuminance(
            original,
            width,
            height,
            x,
            y,
            plan.Options2.Z);
        float shadowWidth = Math.Clamp(plan.Options1.Z, 0, 1);
        float highlightWidth = Math.Clamp(plan.Options2.Y, 0, 1);
        float shadowProtection = shadowWidth <= 0
            ? 0
            : (1 - SmoothStep(
                0,
                shadowWidth,
                shadowLuminance)) *
                Math.Clamp(plan.Options1.Y, 0, 1);
        float highlightProtection = highlightWidth <= 0
            ? 0
            : SmoothStep(
                1 - highlightWidth,
                1,
                highlightLuminance) *
                Math.Clamp(plan.Options2.X, 0, 1);
        float strength =
            MathF.Max(0, plan.Options0.X) *
            (1 - MathF.Max(shadowProtection, highlightProtection));
        Vector3 straight = Vector3.Clamp(
            sourceStraight +
                ((restoredStraight - sourceStraight) * strength),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * source.W, source.W);
    }

    private static float LocalLuminance(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius)
    {
        if (radius <= 0.000001f)
        {
            return Luminance(source[(y * width) + x]);
        }

        float total = Luminance(source[(y * width) + x]);
        const int count = 17;
        for (int index = 1; index < count; index++)
        {
            float fraction = index / (count - 1f);
            float angle = index * 2.39996323f;
            float distance = MathF.Sqrt(fraction) * radius;
            total += Luminance(SampleBilinear(
                source,
                width,
                height,
                x + (MathF.Cos(angle) * distance),
                y + (MathF.Sin(angle) * distance),
                edgeMode: 0));
        }
        return total / count;
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

    internal static Vector4 ContrastAdaptiveSharpen(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0.000001f)
        {
            return center;
        }

        Vector3 centerStraight = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        Vector3 north = SharpenStraight(
            Sample(source, width, height, x, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 west = SharpenStraight(
            Sample(source, width, height, x - 1, y, edgeMode: 0),
            centerStraight);
        Vector3 east = SharpenStraight(
            Sample(source, width, height, x + 1, y, edgeMode: 0),
            centerStraight);
        Vector3 south = SharpenStraight(
            Sample(source, width, height, x, y + 1, edgeMode: 0),
            centerStraight);

        Vector3 straight = ContrastAdaptiveSharpenStraight(
            centerStraight,
            north,
            west,
            east,
            south,
            amount);
        return new Vector4(straight * center.W, center.W);
    }

    internal static Vector4 SobelGatedContrastAdaptiveSharpen(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount,
        float threshold)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0.000001f)
        {
            return center;
        }

        Vector3 centerStraight = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        Vector3 northWest = SharpenStraight(
            Sample(source, width, height, x - 1, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 north = SharpenStraight(
            Sample(source, width, height, x, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 northEast = SharpenStraight(
            Sample(source, width, height, x + 1, y - 1, edgeMode: 0),
            centerStraight);
        Vector3 west = SharpenStraight(
            Sample(source, width, height, x - 1, y, edgeMode: 0),
            centerStraight);
        Vector3 east = SharpenStraight(
            Sample(source, width, height, x + 1, y, edgeMode: 0),
            centerStraight);
        Vector3 southWest = SharpenStraight(
            Sample(source, width, height, x - 1, y + 1, edgeMode: 0),
            centerStraight);
        Vector3 south = SharpenStraight(
            Sample(source, width, height, x, y + 1, edgeMode: 0),
            centerStraight);
        Vector3 southEast = SharpenStraight(
            Sample(source, width, height, x + 1, y + 1, edgeMode: 0),
            centerStraight);

        float northWestLuma = Vector3.Dot(northWest, LuminanceWeights);
        float northLuma = Vector3.Dot(north, LuminanceWeights);
        float northEastLuma = Vector3.Dot(northEast, LuminanceWeights);
        float westLuma = Vector3.Dot(west, LuminanceWeights);
        float eastLuma = Vector3.Dot(east, LuminanceWeights);
        float southWestLuma = Vector3.Dot(southWest, LuminanceWeights);
        float southLuma = Vector3.Dot(south, LuminanceWeights);
        float southEastLuma = Vector3.Dot(southEast, LuminanceWeights);
        float gradientX =
            (northEastLuma + (2 * eastLuma) + southEastLuma) -
            (northWestLuma + (2 * westLuma) + southWestLuma);
        float gradientY =
            (southWestLuma + (2 * southLuma) + southEastLuma) -
            (northWestLuma + (2 * northLuma) + northEastLuma);
        float edgeMagnitude = Math.Clamp(
            MathF.Sqrt(
                (gradientX * gradientX) +
                (gradientY * gradientY)) * 0.25f,
            0,
            1);

        float edgeThreshold = Math.Clamp(threshold, 0, 1);
        float knee = MathF.Max(edgeThreshold * 0.5f, 1f / 255f);
        float kneeStart = MathF.Max(0, edgeThreshold - knee);
        float kneeEnd = MathF.Min(1, edgeThreshold + knee);
        float gate = Math.Clamp(
            (edgeMagnitude - kneeStart) / (kneeEnd - kneeStart),
            0,
            1);
        gate = gate * gate * (3 - (2 * gate));

        Vector3 sharpened = ContrastAdaptiveSharpenStraight(
            centerStraight,
            north,
            west,
            east,
            south,
            amount);
        Vector3 straight = Vector3.Lerp(centerStraight, sharpened, gate);
        return new Vector4(straight * center.W, center.W);
    }

    private static Vector3 ContrastAdaptiveSharpenStraight(
        Vector3 center,
        Vector3 north,
        Vector3 west,
        Vector3 east,
        Vector3 south,
        float amount)
    {
        Vector3 minimum = Vector3.Min(
            Vector3.Min(Vector3.Min(north, west), center),
            Vector3.Min(east, south));
        Vector3 maximum = Vector3.Max(
            Vector3.Max(Vector3.Max(north, west), center),
            Vector3.Max(east, south));
        Vector3 amplitude = new(
            SharpenAmplitude(minimum.X, maximum.X),
            SharpenAmplitude(minimum.Y, maximum.Y),
            SharpenAmplitude(minimum.Z, maximum.Z));
        float strength = Math.Clamp(amount, 0, 1);
        float peak = -strength / (8 - (3 * strength));
        Vector3 weight = amplitude * peak;
        Vector3 denominator = Vector3.One + (weight * 4);
        return Vector3.Clamp(
            Vector3.Divide(
                center +
                    ((north + west + east + south) * weight),
                denominator),
            Vector3.Zero,
            Vector3.One);
    }

    private static Vector3 SharpenStraight(
        Vector4 sample,
        Vector3 fallback) =>
        sample.W > 0.000001f
            ? Vector3.Clamp(
                Unpremultiply(sample),
                Vector3.Zero,
                Vector3.One)
            : fallback;

    internal static Vector4 BinomialHighBoost(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float amount)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0.000001f)
        {
            return center;
        }

        Vector3 centerStraight = SharpenStraight(center, Vector3.Zero);
        Vector3 blurred =
            SharpenStraight(
                Sample(source, width, height, x - 1, y - 1, edgeMode: 0),
                centerStraight) +
            (SharpenStraight(
                Sample(source, width, height, x, y - 1, edgeMode: 0),
                centerStraight) * 2) +
            SharpenStraight(
                Sample(source, width, height, x + 1, y - 1, edgeMode: 0),
                centerStraight) +
            (SharpenStraight(
                Sample(source, width, height, x - 1, y, edgeMode: 0),
                centerStraight) * 2) +
            (centerStraight * 4) +
            (SharpenStraight(
                Sample(source, width, height, x + 1, y, edgeMode: 0),
                centerStraight) * 2) +
            SharpenStraight(
                Sample(source, width, height, x - 1, y + 1, edgeMode: 0),
                centerStraight) +
            (SharpenStraight(
                Sample(source, width, height, x, y + 1, edgeMode: 0),
                centerStraight) * 2) +
            SharpenStraight(
                Sample(source, width, height, x + 1, y + 1, edgeMode: 0),
                centerStraight);
        blurred /= 16;
        float strength = Math.Clamp(amount, 0, 1) * 2;
        Vector3 straight = Vector3.Clamp(
            centerStraight + ((centerStraight - blurred) * strength),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(straight * center.W, center.W);
    }

    private static float SharpenAmplitude(
        float minimum,
        float maximum)
    {
        if (maximum <= 0.000001f)
        {
            return 0;
        }

        float headroom = MathF.Min(minimum, 1 - maximum);
        return MathF.Sqrt(Math.Clamp(headroom / maximum, 0, 1));
    }

    private static Vector4 Sharpen(
        Vector4 center,
        Vector4 blurred,
        float amount,
        float threshold)
    {
        float difference = MathF.Abs(
            Vector3.Dot(
                Unpremultiply(center) -
                    Unpremultiply(blurred),
                LuminanceWeights));
        return difference < threshold
            ? center
            : center + ((center - blurred) * amount);
    }

    internal static Vector4 UnsharpHighBoost(
        Vector4 original,
        Vector4 blurred,
        float amount,
        float threshold)
    {
        if (original.W <= 0.000001f)
        {
            return original;
        }

        Vector3 originalStraight = Vector3.Clamp(
            Unpremultiply(original),
            Vector3.Zero,
            Vector3.One);
        Vector3 detail =
            originalStraight - Unpremultiply(blurred);
        float difference = MathF.Abs(
            Vector3.Dot(detail, LuminanceWeights));
        float center = Math.Clamp(threshold, 0, 1);
        float knee = MathF.Max(center * 0.5f, 1f / 255f);
        float kneeStart = MathF.Max(0, center - knee);
        float kneeEnd = MathF.Min(1, center + knee);
        float gate = Math.Clamp(
            (difference - kneeStart) /
                MathF.Max(kneeEnd - kneeStart, 0.000001f),
            0,
            1);
        gate = gate * gate * (3 - (2 * gate));
        Vector3 straight = Vector3.Clamp(
            originalStraight + (detail * amount * gate),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * original.W,
            original.W);
    }

    internal static Vector4 HighPass(
        Vector4 center,
        Vector4 blurred) =>
        new(
            new Vector3(center.W * 0.5f) +
                new Vector3(center.X, center.Y, center.Z) -
                new Vector3(blurred.X, blurred.Y, blurred.Z),
            center.W);

    internal static Vector4 ReplaceOutlier(
        Vector4 center,
        Vector4 median,
        float threshold)
    {
        float difference = MathF.Abs(
            Vector3.Dot(
                Unpremultiply(center) -
                    Unpremultiply(median),
                LuminanceWeights));
        return difference > threshold ? median : center;
    }

    internal static Vector4[] ApplyDomainTransformPass(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] output = new Vector4[source.Length];
        bool horizontal =
            pass.Kind == PrismNeighborhoodPassKind.Horizontal;
        int radius = (int)MathF.Max(
            pass.RadiusX,
            pass.RadiusY);
        int iteration = Math.Clamp(pass.SampleCount, 0, 2);
        float iterationSigma = iteration switch
        {
            0 => plan.Options2.X,
            1 => plan.Options2.Y,
            _ => plan.Options2.Z
        };
        float spatialSigma = MathF.Max(plan.Options1.Y, 0.000001f);
        float preserveDetails = Math.Clamp(plan.Options0.Y, 0, 1);
        float rangeSigma =
            0.025f + (0.175f * (1 - preserveDetails));
        float lumaMix = Math.Clamp(
            MathF.Max(plan.Options0.X, plan.Options0.W) / 3,
            0,
            1);
        float chromaMix = Math.Clamp(
            plan.Options0.Z / 3,
            0,
            1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                if (center.W <= 0.000001f)
                {
                    output[index] = center;
                    continue;
                }

                Vector3 centerYCoCg = RgbToYCoCg(
                    Vector3.Clamp(
                        Unpremultiply(center),
                        Vector3.Zero,
                        Vector3.One));
                Vector3 total = centerYCoCg;
                float totalWeight = 1;

                for (int direction = -1;
                    direction <= 1;
                    direction += 2)
                {
                    Vector3 previous = centerYCoCg;
                    float domainDistance = 0;
                    for (int step = 1; step <= radius; step++)
                    {
                        int sampleX = horizontal
                            ? x + (direction * step)
                            : x;
                        int sampleY = horizontal
                            ? y
                            : y + (direction * step);
                        Vector4 sample = SampleClamped(
                            source,
                            width,
                            height,
                            sampleX,
                            sampleY);
                        Vector3 sampleYCoCg = RgbToYCoCg(
                            Vector3.Clamp(
                                Unpremultiply(sample),
                                Vector3.Zero,
                                Vector3.One));
                        domainDistance += 1 +
                            ((spatialSigma / rangeSigma) *
                                DomainColorDistance(
                                    sampleYCoCg,
                                    previous));
                        previous = sampleYCoCg;
                        float alphaWeight = Math.Clamp(
                            1 - (MathF.Abs(sample.W - center.W) * 8),
                            0,
                            1);
                        float weight = MathF.Exp(
                            -MathF.Sqrt(2) *
                            domainDistance /
                            MathF.Max(iterationSigma, 0.000001f)) *
                            alphaWeight;
                        total += sampleYCoCg * weight;
                        totalWeight += weight;
                    }
                }

                Vector3 filtered = total / totalWeight;
                Vector3 mixed = new(
                    centerYCoCg.X +
                        ((filtered.X - centerYCoCg.X) * lumaMix),
                    centerYCoCg.Y +
                        ((filtered.Y - centerYCoCg.Y) * chromaMix),
                    centerYCoCg.Z +
                        ((filtered.Z - centerYCoCg.Z) * chromaMix));
                Vector3 straight = Vector3.Clamp(
                    YCoCgToRgb(mixed),
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(
                    straight * center.W,
                    center.W);
            }
        }

        return output;
    }

    internal static Vector4[] ApplyJpegDeblockPass(
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] output = new Vector4[source.Length];
        bool horizontal =
            pass.Kind ==
            PrismNeighborhoodPassKind.JpegDeblockHorizontal;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                int coordinate = horizontal ? x : y;
                int phase = coordinate % 8;
                if (center.W <= 0.000001f ||
                    (phase != 0 && phase != 7))
                {
                    output[index] = center;
                    continue;
                }

                int direction = phase == 7 ? 1 : -1;
                int acrossX = horizontal ? x + direction : x;
                int acrossY = horizontal ? y : y + direction;
                int innerX = horizontal ? x - direction : x;
                int innerY = horizontal ? y : y - direction;
                int acrossInnerX =
                    horizontal ? x + (direction * 2) : x;
                int acrossInnerY =
                    horizontal ? y : y + (direction * 2);
                Vector4 across = SampleClamped(
                    source,
                    width,
                    height,
                    acrossX,
                    acrossY);
                Vector4 inner = SampleClamped(
                    source,
                    width,
                    height,
                    innerX,
                    innerY);
                Vector4 acrossInner = SampleClamped(
                    source,
                    width,
                    height,
                    acrossInnerX,
                    acrossInnerY);

                Vector3 centerYCoCg = RgbToYCoCg(
                    Unpremultiply(center));
                Vector3 acrossYCoCg = RgbToYCoCg(
                    Unpremultiply(across));
                float boundary = DomainColorDistance(
                    centerYCoCg,
                    acrossYCoCg);
                float local = MathF.Max(
                    DomainColorDistance(
                        centerYCoCg,
                        RgbToYCoCg(Unpremultiply(inner))),
                    DomainColorDistance(
                        acrossYCoCg,
                        RgbToYCoCg(Unpremultiply(acrossInner))));
                float alphaWeight = Math.Clamp(
                    1 - (MathF.Abs(across.W - center.W) * 8),
                    0,
                    1);
                float gate =
                    Math.Clamp((boundary - local) * 12, 0, 1) *
                    (1 - ReduceNoiseSmoothStep(0.12f, 0.35f, boundary)) *
                    alphaWeight;
                Vector3 straight = Vector3.Clamp(
                    Vector3.Lerp(
                        Unpremultiply(center),
                        Unpremultiply(across),
                        0.35f * gate),
                    Vector3.Zero,
                    Vector3.One);
                output[index] = new Vector4(
                    straight * center.W,
                    center.W);
            }
        }
        return output;
    }

    internal static Vector4 RecombineReduceNoise(
        PrismNeighborhoodPlan plan,
        Vector4 original,
        Vector4 filtered)
    {
        if (original.W <= 0.000001f)
        {
            return original;
        }

        Vector3 originalYCoCg = RgbToYCoCg(
            Vector3.Clamp(
                Unpremultiply(original),
                Vector3.Zero,
                Vector3.One));
        Vector3 filteredYCoCg = RgbToYCoCg(
            Vector3.Clamp(
                Unpremultiply(filtered),
                Vector3.Zero,
                Vector3.One));
        float strength = Math.Clamp(plan.Options0.X, 0, 1);
        float preserve = Math.Clamp(plan.Options0.Y, 0, 1);
        float colorNoise = Math.Clamp(plan.Options0.Z, 0, 1);
        float sharpen = Math.Clamp(plan.Options0.W, 0, 1);
        bool removeJpeg = plan.Options1.X > 0.5f;
        float lumaMix = MathF.Max(
            strength,
            removeJpeg ? 0.65f : 0);
        float chromaMix = MathF.Max(
            colorNoise,
            removeJpeg ? 0.5f : 0);
        float detail = originalYCoCg.X - filteredYCoCg.X;
        float outputY =
            originalYCoCg.X +
            ((filteredYCoCg.X - originalYCoCg.X) * lumaMix) +
            (detail * ((preserve * strength) + (sharpen * 0.5f)));
        Vector3 combined = new(
            outputY,
            originalYCoCg.Y +
                ((filteredYCoCg.Y - originalYCoCg.Y) * chromaMix),
            originalYCoCg.Z +
                ((filteredYCoCg.Z - originalYCoCg.Z) * chromaMix));
        Vector3 straight = Vector3.Clamp(
            YCoCgToRgb(combined),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * original.W,
            original.W);
    }

    private static Vector4 SampleClamped(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y) =>
        source[
            (Math.Clamp(y, 0, height - 1) * width) +
            Math.Clamp(x, 0, width - 1)];

    private static Vector3 RgbToYCoCg(Vector3 rgb)
    {
        float co = rgb.X - rgb.Z;
        float temporary = (rgb.X + rgb.Z) * 0.5f;
        float cg = rgb.Y - temporary;
        return new Vector3(
            temporary + (cg * 0.5f),
            co,
            cg);
    }

    private static Vector3 YCoCgToRgb(Vector3 color)
    {
        float temporary = color.X - (color.Z * 0.5f);
        float green = color.Z + temporary;
        float blue = temporary - (color.Y * 0.5f);
        return new Vector3(
            blue + color.Y,
            green,
            blue);
    }

    private static float DomainColorDistance(
        Vector3 first,
        Vector3 second) =>
        MathF.Abs(first.X - second.X) +
        (0.25f *
            (MathF.Abs(first.Y - second.Y) +
                MathF.Abs(first.Z - second.Z)));

    private static float ReduceNoiseSmoothStep(
        float low,
        float high,
        float value)
    {
        float amount = Math.Clamp(
            (value - low) / MathF.Max(high - low, 0.000001f),
            0,
            1);
        return amount * amount * (3 - (2 * amount));
    }

    private static Vector4 Sample(
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

    private static Vector4 SampleBilinear(
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

    private static float AddNoiseSample(
        int x,
        int y,
        uint seed,
        uint channel,
        bool gaussian)
    {
        if (!gaussian)
        {
            return (AddNoiseUniform(x, y, seed, channel) * 2) - 1;
        }

        uint pair = channel >> 1;
        float first = MathF.Max(
            AddNoiseUniform(x, y, seed, pair * 2),
            1f / 4294967296f);
        float second = AddNoiseUniform(x, y, seed, (pair * 2) + 1);
        float radius = MathF.Sqrt(-2 * MathF.Log(first));
        float angle = 2 * MathF.PI * second;
        return radius * ((channel & 1) == 0
            ? MathF.Cos(angle)
            : MathF.Sin(angle));
    }

    private static float AddNoiseUniform(
        int x,
        int y,
        uint seed,
        uint channel)
    {
        uint input =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            (seed & 0xffffu) ^
            unchecked((seed >> 16) * 0x27d4eb2du) ^
            unchecked(channel * 0xc2b2ae35u);
        uint state = unchecked((input * 747796405u) + 2891336453u);
        uint word = unchecked(
            ((state >> (int)((state >> 28) + 4)) ^ state) *
            277803737u);
        uint value = (word >> 22) ^ word;
        return (float)((value + 0.5) / 4294967296.0);
    }

    private static float Noise(
        int x,
        int y,
        uint seed,
        uint channel)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed ^
            (channel * 0xc2b2ae35u);
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return ((value & 0x00ffffffu) / 8388607.5f) - 1;
    }

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

    private static float Luminance(Vector4 color) =>
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
