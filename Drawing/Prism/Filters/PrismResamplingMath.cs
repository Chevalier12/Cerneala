using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
    public static PrismPremultipliedColor[] Apply(
        PrismResamplingPlan plan,
        ReadOnlySpan<PrismPremultipliedColor> source,
        int width,
        int height,
        PrismColorProfile workingProfile,
        float opacity = 1,
        Func<Vector2, Vector4>? primaryResource = null,
        Func<Vector2, Vector4>? auxiliaryResource = null,
        Vector2? primaryResourceSize = null)
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
                "A resampling plan must contain at least one pass.",
                nameof(plan));
        }
        if (plan.PrimaryResourceRequired && primaryResource is null)
        {
            throw new InvalidOperationException(
                $"Filter '{plan.Filter}' requires its prepared primary resource.");
        }
        if (plan.AuxiliaryResourceRequired && auxiliaryResource is null)
        {
            throw new InvalidOperationException(
                $"Filter '{plan.Filter}' requires its prepared auxiliary resource.");
        }
        if (primaryResourceSize is Vector2 size &&
            (!float.IsFinite(size.X) || !float.IsFinite(size.Y) ||
             size.X <= 0 || size.Y <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(primaryResourceSize));
        }

        if (opacity == 1 &&
            plan.Passes.All(pass => pass.IsNoOp))
        {
            return source.ToArray();
        }

        Vector4[] original = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            original[index] = ToVector4(
                PrismAdjustmentMath.ConvertProfile(
                    source[index],
                    workingProfile,
                    PrismColorProfile.LinearSrgb));
        }

        Vector4[] current = original;
        for (int passIndex = 0;
            passIndex < plan.Passes.Length;
            passIndex++)
        {
            PrismResamplingPass pass = plan.Passes[passIndex];
            if (pass.IsNoOp)
            {
                continue;
            }

            MipLevel[]? transformMipChain =
                plan.Operation == PrismResamplingOperation.Transform ||
                pass.Kind ==
                    PrismResamplingPassKind.NeonPyramidComposite
                    ? BuildMipChain(current, width, height)
                    : null;
            Vector4[] output = new Vector4[current.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output[(y * width) + x] = ApplyPixel(
                        plan,
                        pass,
                        current,
                        original,
                        width,
                        height,
                        workingProfile,
                        x,
                        y,
                        primaryResource,
                        auxiliaryResource,
                        primaryResourceSize ?? new Vector2(width, height),
                        transformMipChain);
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
            result[index] = PrismAdjustmentMath.ConvertProfile(
                ToPremultiplied(blended),
                PrismColorProfile.LinearSrgb,
                workingProfile);
        }
        return result;
    }

    private static Vector4 ApplyPixel(
        PrismResamplingPlan plan,
        PrismResamplingPass pass,
        Vector4[] source,
        Vector4[] original,
        int width,
        int height,
        PrismColorProfile workingProfile,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource,
        Vector2 primaryResourceSize,
        MipLevel[]? transformMipChain)
    {
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector4 center = source[(y * width) + x];
        if (plan.Operation == PrismResamplingOperation.DiffuseGlow)
        {
            return PrismDiffuseGlowFilter.ApplyPass(
                plan, pass, source, original, width, height, x, y, center);
        }

        if (plan.Operation == PrismResamplingOperation.NeonGlow)
        {
            return PrismNeonGlowFilter.ApplyPass(
                plan,
                pass,
                source,
                original,
                width,
                height,
                x,
                y,
                uv,
                center,
                transformMipChain);
        }

        if (plan.Operation == PrismResamplingOperation.LensCorrection)
        {
            return PrismLensCorrectionFilter.Apply(
                plan,
                source,
                width,
                height,
                uv);
        }

        int edgeMode = EdgeMode(plan);
        Vector4 fill = plan.Operation == PrismResamplingOperation.Offset
            ? PrismOffsetFilter.Fill(plan.Options1, workingProfile)
            : Vector4.Zero;
        if (plan.Operation == PrismResamplingOperation.Wave)
        {
            return PrismWaveFilter.Sample(
                plan,
                source,
                width,
                height,
                uv,
                edgeMode,
                fill);
        }

        Vector2 mapped = MapCoordinate(
            plan,
            uv,
            width,
            height,
            x,
            y,
            primaryResource,
            auxiliaryResource,
            primaryResourceSize);
        if (plan.Operation == PrismResamplingOperation.Transform &&
            transformMipChain is not null)
        {
            return PrismTransformFilter.Sample(
                plan,
                transformMipChain,
                uv,
                mapped,
                width,
                height,
                edgeMode,
                fill);
        }
        if (plan.Operation ==
            PrismResamplingOperation.PolarCoordinates)
        {
            return PrismPolarCoordinatesFilter.Sample(
                plan,
                source,
                width,
                height,
                uv,
                mapped);
        }
        if (plan.Operation == PrismResamplingOperation.Twirl)
        {
            return PrismTwirlFilter.Sample(
                plan,
                source,
                width,
                height,
                uv,
                mapped,
                edgeMode,
                fill);
        }
        if (plan.Operation == PrismResamplingOperation.Liquify)
        {
            return PrismLiquifyFilter.Sample(
                plan,
                source,
                width,
                height,
                uv,
                mapped,
                edgeMode,
                fill,
                primaryResource,
                auxiliaryResource);
        }
        return Sample(
            source,
            width,
            height,
            mapped,
            edgeMode,
            fill);
    }

    private static Vector2 MapCoordinate(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource,
        Vector2 primaryResourceSize)
    {
        Vector4 options0 = plan.Options0;
        Vector4 options1 = plan.Options1;
        return plan.Operation switch
        {
            PrismResamplingOperation.Transform =>
                PrismTransformFilter.Map(plan, uv),
            PrismResamplingOperation.AdaptiveWideAngle =>
                PrismAdaptiveWideAngleFilter.Map(plan, uv),
            PrismResamplingOperation.Displace =>
                PrismDisplaceFilter.Map(
                    plan,
                    uv,
                    primaryResource?.Invoke(
                        PrismDisplaceFilter.MapResourceCoordinate(
                            plan,
                            uv,
                            width,
                            height,
                            primaryResourceSize)) ?? default,
                    width,
                    height),
            PrismResamplingOperation.Glass =>
                PrismGlassFilter.Map(
                    plan,
                    uv,
                    x,
                    y,
                    width,
                    height,
                    primaryResource),
            PrismResamplingOperation.OceanRipple =>
                PrismOceanRippleFilter.Map(
                    plan, uv, x, y, width, height),
            PrismResamplingOperation.Pinch =>
                PrismPinchFilter.Map(options0, uv),
            PrismResamplingOperation.PolarCoordinates =>
                PrismPolarCoordinatesFilter.Map(
                    options0, uv, width, height),
            PrismResamplingOperation.Ripple =>
                PrismRippleFilter.Map(plan, uv, width, height),
            PrismResamplingOperation.Shear =>
                PrismShearFilter.Map(options0, uv),
            PrismResamplingOperation.Spherize =>
                PrismSpherizeFilter.Map(options0, uv),
            PrismResamplingOperation.Twirl =>
                PrismTwirlFilter.Map(options0, uv),
            PrismResamplingOperation.Wave =>
                PrismWaveFilter.Map(plan, uv, width, height),
            PrismResamplingOperation.ZigZag =>
                PrismZigZagFilter.Map(
                    options0,
                    options1,
                    uv,
                    width,
                    height),
            PrismResamplingOperation.Liquify =>
                PrismLiquifyFilter.Map(
                    plan,
                    uv,
                    primaryResource?.Invoke(uv) ??
                        new Vector4(0.5f, 0.5f, 0, 1),
                    auxiliaryResource?.Invoke(uv)),
            PrismResamplingOperation.Offset =>
                PrismOffsetFilter.Map(options0, uv, width, height),
            _ => uv
        };
    }

    private static float SmoothStep(
        float edge0,
        float edge1,
        float value)
    {
        float normalized = Math.Clamp(
            (value - edge0) /
            MathF.Max(edge1 - edge0, 0.0001f),
            0,
            1);
        return normalized * normalized *
            (3 - (2 * normalized));
    }

    internal static int EdgeMode(
        PrismResamplingPlan plan) =>
        plan.Operation switch
        {
            PrismResamplingOperation.Transform =>
                (int)plan.Options2.Z,
            PrismResamplingOperation.AdaptiveWideAngle => 1,
            PrismResamplingOperation.LensCorrection =>
                (int)plan.Options2.Y,
            PrismResamplingOperation.Displace =>
                (int)plan.Options0.W,
            PrismResamplingOperation.PolarCoordinates => 1,
            PrismResamplingOperation.Ripple =>
                (int)plan.Options1.X,
            PrismResamplingOperation.Shear =>
                (int)plan.Options0.Z,
            PrismResamplingOperation.Wave =>
                (int)plan.Options2.X,
            PrismResamplingOperation.Liquify =>
                (int)plan.Options0.Z,
            PrismResamplingOperation.Offset =>
                (int)plan.Options0.Z,
            _ => 0
        };

    private static float Channel(
        Vector4 value,
        int channel) =>
        channel switch
        {
            0 => value.X,
            1 => value.Y,
            2 => value.Z,
            3 => value.W,
            _ => Vector3.Dot(
                new Vector3(
                    value.X,
                    value.Y,
                    value.Z),
                new Vector3(
                    0.2126f,
                    0.7152f,
                    0.0722f))
        };

    private static uint Seed(float low, float high) =>
        ((uint)high << 16) | (uint)low;

    private static Vector2 Rotate(
        Vector2 value,
        float angle)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(
            (value.X * cosine) -
                (value.Y * sine),
            (value.X * sine) +
                (value.Y * cosine));
    }

    private static Vector2 Fract(Vector2 value) =>
        new(
            value.X - MathF.Floor(value.X),
            value.Y - MathF.Floor(value.Y));

    private static float Mirror(float value) =>
        1 -
        MathF.Abs(
            ((value * 0.5f -
                MathF.Floor(value * 0.5f)) * 2) -
            1);

    private static int Wrap(int value, int length)
    {
        int wrapped = value % length;
        return wrapped < 0
            ? wrapped + length
            : wrapped;
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

    private static float NonZero(float value) =>
        MathF.Abs(value) < 0.000001f
            ? MathF.CopySign(0.000001f, value)
            : value;

    internal static Vector4 AssociatedFill(
        Vector4 straight,
        PrismColorProfile workingProfile)
    {
        PrismPremultipliedColor working =
            PrismPremultipliedColor.FromStraight(
                straight.X,
                straight.Y,
                straight.Z,
                straight.W);
        return ToVector4(
            PrismAdjustmentMath.ConvertProfile(
                working,
                workingProfile,
                PrismColorProfile.LinearSrgb));
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

    private static Vector3 Unpremultiply(
        Vector4 color) =>
        color.W <= 0
            ? Vector3.Zero
            : new Vector3(
                color.X,
                color.Y,
                color.Z) / color.W;

    private static Vector4 ToVector4(
        PrismPremultipliedColor color) =>
        new(
            (float)color.Red,
            (float)color.Green,
            (float)color.Blue,
            (float)color.Alpha);

    private static PrismPremultipliedColor ToPremultiplied(
        Vector4 color) =>
        new(
            color.X,
            color.Y,
            color.Z,
            color.W);

}
