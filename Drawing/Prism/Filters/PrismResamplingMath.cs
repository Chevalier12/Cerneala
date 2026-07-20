using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismResamplingMath
{
    public static PrismPremultipliedColor[] Apply(
        PrismResamplingPlan plan,
        ReadOnlySpan<PrismPremultipliedColor> source,
        int width,
        int height,
        PrismColorProfile workingProfile,
        float opacity = 1,
        Func<Vector2, Vector4>? primaryResource = null,
        Func<Vector2, Vector4>? auxiliaryResource = null)
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

            Vector4[] output = new Vector4[current.Length];
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
                        workingProfile,
                        x,
                        y,
                        primaryResource,
                        auxiliaryResource);
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
        int width,
        int height,
        PrismColorProfile workingProfile,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector2 uv = new(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
        Vector4 center = source[(y * width) + x];
        if (plan.Operation == PrismResamplingOperation.DiffuseGlow)
        {
            return pass.Kind == PrismResamplingPassKind.Diffuse
                ? Diffuse(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y)
                : Grain(plan, center, x, y);
        }

        Vector2 mapped = MapCoordinate(
            plan,
            uv,
            width,
            height,
            x,
            y,
            primaryResource,
            auxiliaryResource);
        int edgeMode = EdgeMode(plan);
        Vector4 fill = plan.Operation == PrismResamplingOperation.Offset
            ? AssociatedFill(
                plan.Options1,
                workingProfile)
            : Vector4.Zero;
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
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector4 options0 = plan.Options0;
        Vector4 options1 = plan.Options1;
        return plan.Operation switch
        {
            PrismResamplingOperation.Transform =>
                MapTransform(plan, uv),
            PrismResamplingOperation.AdaptiveWideAngle =>
                MapAdaptiveWideAngle(
                    plan,
                    uv,
                    primaryResource?.Invoke(uv) ??
                        new Vector4(0.5f, 0.5f, 0, 1),
                    width,
                    height),
            PrismResamplingOperation.LensCorrection =>
                MapLensCorrection(plan, uv),
            PrismResamplingOperation.Displace =>
                MapDisplace(
                    plan,
                    uv,
                    primaryResource?.Invoke(
                        options0.Z < 0.5f
                            ? uv
                            : Fract(uv)) ?? default,
                    width,
                    height),
            PrismResamplingOperation.Glass =>
                MapGlass(
                    plan,
                    uv,
                    x,
                    y,
                    primaryResource),
            PrismResamplingOperation.OceanRipple =>
                MapOceanRipple(plan, uv, x, y, width, height),
            PrismResamplingOperation.Pinch =>
                MapPinch(options0, uv),
            PrismResamplingOperation.PolarCoordinates =>
                MapPolar(options0, uv),
            PrismResamplingOperation.Ripple =>
                MapRipple(plan, uv, x, y, width),
            PrismResamplingOperation.Shear =>
                MapShear(options0, uv),
            PrismResamplingOperation.Spherize =>
                MapSpherize(options0, uv),
            PrismResamplingOperation.Twirl =>
                MapTwirl(options0, uv),
            PrismResamplingOperation.Wave =>
                MapWave(plan, uv, x, y, width, height),
            PrismResamplingOperation.ZigZag =>
                MapZigZag(options0, options1, uv, width),
            PrismResamplingOperation.Liquify =>
                MapLiquify(
                    plan,
                    uv,
                    primaryResource?.Invoke(uv) ??
                        new Vector4(0.5f, 0.5f, 0, 1),
                    auxiliaryResource?.Invoke(uv)),
            PrismResamplingOperation.Offset =>
                uv - new Vector2(
                    options0.X / width,
                    options0.Y / height),
            _ => uv
        };
    }

    private static Vector2 MapTransform(
        PrismResamplingPlan plan,
        Vector2 uv)
    {
        Vector2 origin = new(
            plan.Options2.X,
            plan.Options2.Y);
        Vector2 size = new(
            MathF.Max(plan.Options3.X, 1),
            MathF.Max(plan.Options3.Y, 1));
        Vector2 position =
            uv -
            origin -
            (new Vector2(
                plan.Options0.X,
                plan.Options0.Y) / size);
        position = Rotate(position, -plan.Options1.X);
        float skewX = plan.Options1.Y;
        float skewY = plan.Options1.Z;
        float determinant =
            1 - (skewX * skewY);
        determinant = MathF.Abs(determinant) < 0.000001f
            ? MathF.CopySign(0.000001f, determinant)
            : determinant;
        position = new Vector2(
            position.X - (skewX * position.Y),
            position.Y - (skewY * position.X)) /
            determinant;
        Vector2 scale = new(
            NonZero(plan.Options0.Z),
            NonZero(plan.Options0.W));
        return origin + (position / scale);
    }

    private static Vector2 MapAdaptiveWideAngle(
        PrismResamplingPlan plan,
        Vector2 uv,
        Vector4 constraint,
        int width,
        int height)
    {
        Vector2 centered = uv - new Vector2(0.5f);
        float radius = centered.LengthSquared();
        float projection =
            1 + (plan.Options0.X * 0.12f);
        float focal = plan.Options0.Y > 0.5f
            ? 0.75f
            : 1;
        centered *=
            1 +
            (radius * projection * focal /
                MathF.Max(plan.Options0.Z, 0.0001f));
        centered = Rotate(centered, -plan.Options1.X);
        return new Vector2(0.5f) +
            (centered /
                MathF.Max(plan.Options0.W, 0.0001f)) -
            new Vector2(
                plan.Options1.Y,
                plan.Options1.Z) /
                new Vector2(width, height) +
            (new Vector2(
                constraint.X,
                constraint.Y) * 2 -
                Vector2.One) * 0.001f;
    }

    private static Vector2 MapLensCorrection(
        PrismResamplingPlan plan,
        Vector2 uv)
    {
        Vector2 centered = Rotate(
            uv - new Vector2(0.5f),
            -plan.Options1.W);
        centered.X -= plan.Options1.Z * centered.Y;
        centered.Y -= plan.Options1.Y * centered.X;
        centered *=
            (1 +
                (plan.Options0.X *
                    centered.LengthSquared())) /
            MathF.Max(plan.Options2.X, 0.0001f);
        return centered + new Vector2(0.5f);
    }

    private static Vector2 MapDisplace(
        PrismResamplingPlan plan,
        Vector2 uv,
        Vector4 map,
        int width,
        int height)
    {
        Vector2 displacement = new(
            Channel(map, (int)plan.Options1.X),
            Channel(map, (int)plan.Options1.Y));
        displacement = (displacement * 2) - Vector2.One;
        return uv -
            new Vector2(
                displacement.X *
                    plan.Options0.X / width,
                displacement.Y *
                    plan.Options0.Y / height);
    }

    private static Vector2 MapGlass(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        Func<Vector2, Vector4>? primaryResource)
    {
        Vector4 texture = primaryResource?.Invoke(uv) ??
            new Vector4(
                Hash(x, y, (uint)plan.Options0.Z),
                Hash(x, y, (uint)plan.Options0.Z + 1),
                0.5f,
                1);
        Vector2 displacement =
            (new Vector2(texture.X, texture.Y) * 2) -
            Vector2.One;
        if (plan.Options1.X > 0.5f)
        {
            displacement = -displacement;
        }
        return uv -
            (displacement *
                plan.Options0.X *
                MathF.Max(plan.Options0.W, 0.0001f) *
                0.001f);
    }

    private static Vector2 MapOceanRipple(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width,
        int height)
    {
        uint seed = Seed(plan.Options0.Z, plan.Options0.W);
        float size = MathF.Max(plan.Options0.X, 0.0001f);
        float noise = Hash(
            (int)MathF.Floor(x / size),
            (int)MathF.Floor(y / size),
            seed);
        Vector2 position = new(x / size, y / size);
        return uv + new Vector2(
            MathF.Sin((position.Y + noise) * MathF.Tau) *
                plan.Options0.Y / width,
            MathF.Cos((position.X - noise) * MathF.Tau) *
                plan.Options0.Y / height);
    }

    private static Vector2 MapPinch(
        Vector4 options,
        Vector2 uv)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 delta = uv - center;
        float radius =
            delta.Length() / 0.70710678f;
        float factor = MathF.Pow(
            MathF.Max(radius, 0.000001f),
            options.X);
        return center + (delta * factor);
    }

    private static Vector2 MapPolar(
        Vector4 options,
        Vector2 uv)
    {
        Vector2 center = new(options.Y, options.Z);
        if (options.X < 0.5f)
        {
            float angle =
                (uv.X - center.X) * MathF.Tau;
            float radius =
                (uv.Y - center.Y + 0.5f) *
                0.70710678f;
            return center + new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle)) * radius;
        }

        Vector2 delta = uv - center;
        return new Vector2(
            center.X +
                (MathF.Atan2(delta.Y, delta.X) /
                    MathF.Tau),
            center.Y - 0.5f +
                (delta.Length() / 0.70710678f));
    }

    private static Vector2 MapRipple(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width)
    {
        uint seed = Seed(plan.Options0.Z, plan.Options0.W);
        float size = 6 + (plan.Options0.Y * 8);
        float phase =
            (uv.Y * size) +
            (Hash(
                (int)MathF.Floor(x / size),
                (int)MathF.Floor(y / size),
                seed) * MathF.Tau);
        return uv + new Vector2(
            MathF.Sin(phase) *
                plan.Options0.X / width,
            0);
    }

    private static Vector2 MapShear(
        Vector4 options,
        Vector2 uv)
    {
        float y = Math.Clamp(uv.Y, 0, 1);
        float amount = (int)options.X switch
        {
            0 => 0,
            1 => y * y,
            2 => 1 - ((1 - y) * (1 - y)),
            3 => y * y * (3 - (2 * y)),
            _ => MathF.Sin((y - 0.5f) * MathF.PI)
        };
        return uv - new Vector2(
            (amount - 0.5f) * 0.5f,
            0);
    }

    private static Vector2 MapSpherize(
        Vector4 options,
        Vector2 uv)
    {
        Vector2 center = new(options.Z, options.W);
        Vector2 delta = uv - center;
        float radius = Math.Clamp(
            delta.Length() / 0.70710678f,
            0,
            1);
        float factor = MathF.Max(
            1 +
                (options.X *
                    (1 - (radius * radius))),
            0.0001f);
        Vector2 warped = delta / factor;
        if (options.Y is > 0.5f and < 1.5f)
        {
            warped.Y = delta.Y;
        }
        else if (options.Y > 1.5f)
        {
            warped.X = delta.X;
        }
        return center + warped;
    }

    private static Vector2 MapTwirl(
        Vector4 options,
        Vector2 uv)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 delta = uv - center;
        float radius =
            delta.Length() / 0.70710678f;
        return center + Rotate(
            delta,
            -options.X *
                Math.Clamp(1 - radius, 0, 1));
    }

    private static Vector2 MapWave(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width,
        int height)
    {
        uint seed = Seed(
            plan.Options2.Y,
            plan.Options2.Z);
        float phase = Hash(x, y, seed);
        float generators =
            MathF.Max(plan.Options0.X, 1);
        int kind = (int)plan.Options0.W;
        float waveX = WaveShape(
            (uv.Y * generators /
                MathF.Max(
                    plan.Options0.Y / height,
                    0.000001f)) + phase,
            kind);
        float waveY = WaveShape(
            (uv.X * generators /
                MathF.Max(
                    plan.Options0.Z / width,
                    0.000001f)) - phase,
            kind);
        return uv + new Vector2(
            waveX * plan.Options1.X *
                plan.Options1.Z / width,
            waveY * plan.Options1.Y *
                plan.Options1.W / height);
    }

    private static Vector2 MapZigZag(
        Vector4 options0,
        Vector4 options1,
        Vector2 uv,
        int width)
    {
        Vector2 center = new(
            options1.X,
            options1.Y);
        Vector2 delta = uv - center;
        float radius = delta.Length();
        float angle = MathF.Atan2(
            delta.Y,
            delta.X);
        float ridges = MathF.Max(
            options0.Y,
            1);
        float oscillation = MathF.Sin(
            (radius * ridges * 40) +
            (options0.Z > 1.5f
                ? angle * ridges
                : 0));
        float amount =
            oscillation *
            options0.X /
            width;
        if (options0.Z < 0.5f)
        {
            Vector2 direction = delta.LengthSquared() > 0
                ? Vector2.Normalize(delta)
                : Vector2.UnitX;
            return center +
                (direction * (radius + amount));
        }

        angle += amount * 10;
        return center + new Vector2(
            MathF.Cos(angle),
            MathF.Sin(angle)) * radius;
    }

    private static Vector2 MapLiquify(
        PrismResamplingPlan plan,
        Vector2 uv,
        Vector4 mesh,
        Vector4? maskSample)
    {
        Vector2 displacement =
            (new Vector2(mesh.X, mesh.Y) * 2) -
            Vector2.One;
        float mask = maskSample?.W ?? 1;
        if (plan.Options0.Y > 0.5f)
        {
            mask = 1 - mask;
        }
        return uv -
            (displacement *
                (1 - Math.Clamp(plan.Options0.X, 0, 1)) *
                mask);
    }

    private static Vector4 Diffuse(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float radius = MathF.Max(
            plan.Options0.Y,
            0.5f);
        Vector4 total =
            SamplePixel(source, width, height, x, y, 0) * 4 +
            SamplePixel(source, width, height, x + radius, y, 0) +
            SamplePixel(source, width, height, x - radius, y, 0) +
            SamplePixel(source, width, height, x, y + radius, 0) +
            SamplePixel(source, width, height, x, y - radius, 0);
        return total / 8;
    }

    private static Vector4 Grain(
        PrismResamplingPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        float noise = Hash(x, y, 9173) - 0.5f;
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(center) +
                new Vector3(noise * plan.Options0.X) +
                (new Vector3(
                    plan.Options1.X,
                    plan.Options1.Y,
                    plan.Options1.Z) *
                    plan.Options0.Z),
            Vector3.Zero,
            Vector3.One);
        return new Vector4(
            straight * center.W,
            center.W);
    }

    private static Vector4 Sample(
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        int edgeMode,
        Vector4 fill)
    {
        bool outside = uv.X < 0 ||
            uv.X > 1 ||
            uv.Y < 0 ||
            uv.Y > 1;
        if (outside && edgeMode == 1)
        {
            return Vector4.Zero;
        }
        if (outside && edgeMode == 4)
        {
            return fill;
        }

        if (edgeMode == 2)
        {
            uv = Fract(uv);
        }
        else if (edgeMode == 3)
        {
            uv = new Vector2(
                Mirror(uv.X),
                Mirror(uv.Y));
        }

        float sampleX = (uv.X * width) - 0.5f;
        float sampleY = (uv.Y * height) - 0.5f;
        int x0 = (int)MathF.Floor(sampleX);
        int y0 = (int)MathF.Floor(sampleY);
        float fractionX = sampleX - x0;
        float fractionY = sampleY - y0;
        Vector4 top = Vector4.Lerp(
            SamplePixel(
                source,
                width,
                height,
                x0,
                y0,
                edgeMode),
            SamplePixel(
                source,
                width,
                height,
                x0 + 1,
                y0,
                edgeMode),
            fractionX);
        Vector4 bottom = Vector4.Lerp(
            SamplePixel(
                source,
                width,
                height,
                x0,
                y0 + 1,
                edgeMode),
            SamplePixel(
                source,
                width,
                height,
                x0 + 1,
                y0 + 1,
                edgeMode),
            fractionX);
        return Vector4.Lerp(
            top,
            bottom,
            fractionY);
    }

    private static Vector4 SamplePixel(
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

    private static int EdgeMode(
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
            PrismResamplingOperation.PolarCoordinates => 2,
            PrismResamplingOperation.Ripple =>
                (int)plan.Options1.X,
            PrismResamplingOperation.Shear =>
                (int)plan.Options0.Y,
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

    private static float WaveShape(
        float phase,
        int kind)
    {
        float value = phase - MathF.Floor(phase);
        return kind switch
        {
            1 => (MathF.Abs(value - 0.5f) * 4) - 1,
            2 => value >= 0.5f ? 1 : -1,
            _ => MathF.Sin(phase * MathF.Tau)
        };
    }

    private static float Hash(
        int x,
        int y,
        uint seed)
    {
        uint value =
            unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            seed;
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) /
            16777215f;
    }

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

    private static Vector4 AssociatedFill(
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
