using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismNeighborhoodMath
{
    private static readonly Vector3 LuminanceWeights =
        new(0.2126f, 0.7152f, 0.0722f);

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

        Vector4[] current = original;
        for (int passIndex = 0;
            passIndex < plan.Passes.Length;
            passIndex++)
        {
            PrismNeighborhoodPass pass = plan.Passes[passIndex];
            if (pass.IsNoOp)
            {
                continue;
            }

            Vector4[] output = new Vector4[current.Length];
            if (plan.Operation == PrismNeighborhoodOperation.Average)
            {
                Vector4 average = Vector4.Zero;
                for (int index = 0; index < current.Length; index++)
                {
                    average += current[index];
                }
                average /= current.Length;
                Array.Fill(output, average);
            }
            else
            {
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
            }
            current = output;
        }

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
            PrismNeighborhoodOperation.Blur =>
                SampleOptimizedBilinearGaussian(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode),
            PrismNeighborhoodOperation.BlurMore =>
                SampleDisk(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode),
            PrismNeighborhoodOperation.BoxBlur =>
                SampleLine(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode,
                    gaussian: false),
            PrismNeighborhoodOperation.GaussianBlur =>
                SampleLine(
                    source,
                    width,
                    height,
                    x,
                    y,
                    pass,
                    edgeMode,
                    gaussian: true),
            PrismNeighborhoodOperation.MotionBlur =>
                SampleMotion(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    edgeMode),
            PrismNeighborhoodOperation.SmartBlur or
            PrismNeighborhoodOperation.SurfaceBlur =>
                SampleEdgeAware(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y),
            PrismNeighborhoodOperation.Sharpen =>
                Sharpen(
                    center,
                    Neighborhood3x3(source, width, height, x, y),
                    plan.Options0.X,
                    0),
            PrismNeighborhoodOperation.SharpenMore =>
                Sharpen(
                    center,
                    Neighborhood3x3(source, width, height, x, y),
                    plan.Options0.X * 2,
                    0),
            PrismNeighborhoodOperation.SharpenEdges =>
                Sharpen(
                    center,
                    Neighborhood3x3(source, width, height, x, y),
                    plan.Options0.X,
                    plan.Options0.Y),
            PrismNeighborhoodOperation.UnsharpMask =>
                Sharpen(
                    center,
                    SampleDisk(
                        source,
                        width,
                        height,
                        x,
                        y,
                        pass,
                        edgeMode: 0),
                    plan.Options0.X,
                    plan.Options0.Z),
            PrismNeighborhoodOperation.SmartSharpen =>
                SmartSharpen(
                    plan,
                    center,
                    SampleDisk(
                        source,
                        width,
                        height,
                        x,
                        y,
                        pass,
                        edgeMode: 0)),
            PrismNeighborhoodOperation.HighPass =>
                HighPass(
                    center,
                    SampleDisk(
                        source,
                        width,
                        height,
                        x,
                        y,
                        pass,
                        edgeMode)),
            PrismNeighborhoodOperation.AddNoise =>
                AddNoise(plan, center, x, y),
            PrismNeighborhoodOperation.Despeckle =>
                ReplaceOutlier(
                    center,
                    Median3x3(source, width, height, x, y),
                    plan.Options0.X),
            PrismNeighborhoodOperation.DustScratches =>
                ReplaceOutlier(
                    center,
                    Median3x3(source, width, height, x, y),
                    plan.Options0.Y),
            PrismNeighborhoodOperation.Median =>
                Median3x3(source, width, height, x, y),
            PrismNeighborhoodOperation.ReduceNoise =>
                ReduceNoise(
                    plan,
                    center,
                    Neighborhood3x3(source, width, height, x, y)),
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
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        float amount = 1;
        if (plan.Operation == PrismNeighborhoodOperation.FieldBlur)
        {
            amount = Math.Clamp(resource?.Invoke(uv).X ?? 0, 0, 1);
        }
        else if (plan.Operation == PrismNeighborhoodOperation.IrisBlur)
        {
            Vector2 delta = new(
                (uv.X - plan.Options0.X) /
                    MathF.Max(plan.Options0.Z, 0.000001f),
                (uv.Y - plan.Options0.Y) /
                    MathF.Max(plan.Options0.W, 0.000001f));
            amount = Math.Clamp(
                (delta.Length() - 1) /
                    MathF.Max(plan.Options1.X, 0.000001f),
                0,
                1);
        }
        else if (plan.Operation == PrismNeighborhoodOperation.TiltShift)
        {
            Vector2 direction = new(
                -MathF.Sin(plan.Options0.Z),
                MathF.Cos(plan.Options0.Z));
            float distance = MathF.Abs(
                Vector2.Dot(
                    uv - new Vector2(
                        plan.Options0.X,
                        plan.Options0.Y),
                    direction));
            amount = Math.Clamp(
                (distance - plan.Options0.W) /
                    MathF.Max(plan.Options1.X, 0.000001f),
                0,
                1);
        }

        PrismNeighborhoodPass adjusted = pass with
        {
            RadiusX = MathF.Max(
                pass.RadiusX * amount,
                plan.Operation == PrismNeighborhoodOperation.FieldBlur
                    ? amount * 24
                    : 0),
            RadiusY = MathF.Max(
                pass.RadiusY * amount,
                plan.Operation == PrismNeighborhoodOperation.FieldBlur
                    ? amount * 24
                    : 0)
        };
        Vector4 blurred = plan.Operation switch
        {
            PrismNeighborhoodOperation.PathBlur =>
                SamplePath(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y,
                    resource),
            PrismNeighborhoodOperation.SpinBlur or
            PrismNeighborhoodOperation.RadialBlur =>
                SampleRadial(
                    plan,
                    pass,
                    source,
                    width,
                    height,
                    x,
                    y),
            _ => SampleDisk(
                source,
                width,
                height,
                x,
                y,
                adjusted,
                EdgeMode(plan))
        };
        return Vector4.Lerp(center, blurred, amount);
    }

    private static Vector4 SampleMotion(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int edgeMode)
    {
        float distance = plan.Options0.X;
        float angle = plan.Options0.Y;
        PrismNeighborhoodPass directed = pass with
        {
            RadiusX = MathF.Cos(angle) * distance,
            RadiusY = -MathF.Sin(angle) * distance
        };
        return SampleLine(
            source,
            width,
            height,
            x,
            y,
            directed,
            edgeMode,
            gaussian: false);
    }

    private static Vector4 SamplePath(
        PrismNeighborhoodPlan plan,
        PrismNeighborhoodPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? resource)
    {
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector4 path =
            resource?.Invoke(uv) ?? new Vector4(0.5f, 0.5f, 0, 1);
        Vector2 direction =
            new(path.X * 2 - 1, path.Y * 2 - 1);
        if (direction.LengthSquared() <= 0.000001f)
        {
            direction = Vector2.UnitX;
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }
        float pathAmount = Math.Clamp(path.Z, 0, 1);
        float speed = plan.Options0.X +
            ((plan.Options0.W - plan.Options0.X) * pathAmount);
        return SampleLine(
            source,
            width,
            height,
            x,
            y,
            pass with
            {
                RadiusX = direction.X * speed,
                RadiusY = direction.Y * speed
            },
            edgeMode: 0,
            gaussian: false);
    }

    private static Vector4 SampleRadial(
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
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = Vector4.Zero;
        for (int index = 0; index < count; index++)
        {
            float position = count <= 1
                ? 0
                : ((float)index / (count - 1)) - 0.5f;
            float angle = position * amount;
            float cosine = MathF.Cos(angle);
            float sine = MathF.Sin(angle);
            Vector2 sampleUv = center + new Vector2(
                (delta.X * cosine) - (delta.Y * sine),
                (delta.X * sine) + (delta.Y * cosine));
            total += Sample(
                source,
                width,
                height,
                (sampleUv.X * width) - 0.5f,
                (sampleUv.Y * height) - 0.5f,
                edgeMode: 0);
        }
        return total / count;
    }

    private static Vector4 SampleLine(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismNeighborhoodPass pass,
        int edgeMode,
        bool gaussian)
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
            total += Sample(
                source,
                width,
                height,
                x + (pass.RadiusX * position),
                y + (pass.RadiusY * position),
                edgeMode) * weight;
            totalWeight += weight;
        }
        return total / MathF.Max(totalWeight, 0.000001f);
    }

    private static Vector4 SampleDisk(
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

    private static Vector4 SampleOptimizedBilinearGaussian(
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

    private static Vector4 SampleEdgeAware(
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
        float threshold = plan.Options0.Y;
        int count = Math.Max(1, pass.SampleCount);
        Vector4 total = center;
        int accepted = 1;
        for (int index = 1; index < count; index++)
        {
            float fraction = (float)index / Math.Max(count - 1, 1);
            float angle = index * 2.39996323f;
            Vector4 sample = Sample(
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
                edgeMode: 0);
            float difference = MathF.Abs(
                Vector3.Dot(
                    Unpremultiply(sample) - centerStraight,
                    LuminanceWeights));
            if (difference <= threshold)
            {
                total += sample;
                accepted++;
            }
        }
        Vector4 result = total / accepted;
        if (plan.Operation == PrismNeighborhoodOperation.SmartBlur &&
            plan.Options0.W > 0.5f)
        {
            float edge = Math.Clamp(
                Vector3.Distance(
                    new Vector3(center.X, center.Y, center.Z),
                    new Vector3(result.X, result.Y, result.Z)) * 4,
                0,
                1);
            Vector4 edgeColor = new(edge, edge, edge, center.W);
            return plan.Options0.W > 1.5f
                ? Vector4.Lerp(center, edgeColor, edge)
                : edgeColor;
        }
        return result;
    }

    private static Vector4 Neighborhood3x3(
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

    private static Vector4 Median3x3(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Span<Vector4> values = stackalloc Vector4[9];
        int index = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                values[index++] = Sample(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY,
                    edgeMode: 0);
            }
        }
        values.Sort(
            static (left, right) =>
                Luminance(left).CompareTo(Luminance(right)));
        return values[4];
    }

    private static Vector4 AddNoise(
        PrismNeighborhoodPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        uint seed =
            ((uint)plan.Options1.X << 16) |
            (uint)plan.Options0.W;
        float red = Noise(x, y, seed, 0);
        float green = plan.Options0.Z > 0.5f
            ? red
            : Noise(x, y, seed, 1);
        float blue = plan.Options0.Z > 0.5f
            ? red
            : Noise(x, y, seed, 2);
        Vector3 noise = new(red, green, blue);
        if (plan.Options0.Y > 0.5f)
        {
            noise = (
                noise +
                new Vector3(
                    Noise(x, y, seed, 3),
                    Noise(x, y, seed, 4),
                    Noise(x, y, seed, 5))) * 0.5f;
        }
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(center) +
                (noise * plan.Options0.X),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * center.W,
            center.W);
    }

    private static Vector4 SmartSharpen(
        PrismNeighborhoodPlan plan,
        Vector4 center,
        Vector4 blurred)
    {
        Vector4 sharpened = Sharpen(
            center,
            blurred,
            plan.Options0.X,
            0);
        return Vector4.Lerp(
            sharpened,
            blurred,
            Math.Clamp(plan.Options0.Z, 0, 1));
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

    private static Vector4 HighPass(
        Vector4 center,
        Vector4 blurred) =>
        new(
            new Vector3(center.W * 0.5f) +
                new Vector3(center.X, center.Y, center.Z) -
                new Vector3(blurred.X, blurred.Y, blurred.Z),
            center.W);

    private static Vector4 ReplaceOutlier(
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

    private static Vector4 ReduceNoise(
        PrismNeighborhoodPlan plan,
        Vector4 center,
        Vector4 denoised)
    {
        Vector4 preserved = Vector4.Lerp(
            denoised,
            center,
            Math.Clamp(plan.Options0.Y, 0, 1));
        Vector4 sharpened = Sharpen(
            preserved,
            denoised,
            plan.Options0.W,
            0);
        return Vector4.Lerp(
            center,
            sharpened,
            Math.Clamp(plan.Options0.X, 0, 1));
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

    private static int EdgeMode(PrismNeighborhoodPlan plan) =>
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
            _ => 0
        };

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
