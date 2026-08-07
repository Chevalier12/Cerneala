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

    internal static Vector2 MapTransform(
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

    internal static Vector2 MapAdaptiveWideAngle(
        PrismResamplingPlan plan,
        Vector2 uv)
    {
        Vector2 focalLength = new(
            plan.Options0.X,
            plan.Options0.Y);
        Vector2 principalPoint = new(
            plan.Options0.Z,
            plan.Options0.W);
        Vector2 normalized =
            (uv - principalPoint) / focalLength;
        float radius = normalized.Length();
        if (radius < 0.000001f)
        {
            return uv;
        }

        float theta = MathF.Atan(radius);
        float theta2 = theta * theta;
        float theta4 = theta2 * theta2;
        float theta6 = theta4 * theta2;
        float theta8 = theta4 * theta4;
        Vector4 coefficients = plan.Options1;
        float distortedTheta = theta *
            (1 +
                (coefficients.X * theta2) +
                (coefficients.Y * theta4) +
                (coefficients.Z * theta6) +
                (coefficients.W * theta8));
        float radialScale = distortedTheta / radius;
        return principalPoint +
            (normalized * radialScale * focalLength);
    }

    internal static Vector4 ApplyLensCorrection(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv)
    {
        int edgeMode = EdgeMode(plan);
        float redCyan = plan.Options0.Y;
        float blueYellow = plan.Options0.Z;
        if (redCyan == 0 && blueYellow == 0)
        {
            Vector4 sampled = Sample(
                source,
                width,
                height,
                MapLensCorrection(plan, uv, width, height, 0),
                edgeMode,
                Vector4.Zero);
            return ApplyLensVignette(plan, uv, width, height, sampled);
        }

        float redShift = 0.01f *
            (redCyan - (blueYellow * 0.5f));
        float greenShift = -0.005f *
            (redCyan + blueYellow);
        float blueShift = 0.01f *
            (blueYellow - (redCyan * 0.5f));
        Vector4 red = Sample(
            source,
            width,
            height,
            MapLensCorrection(plan, uv, width, height, redShift),
            edgeMode,
            Vector4.Zero);
        Vector4 green = Sample(
            source,
            width,
            height,
            MapLensCorrection(plan, uv, width, height, greenShift),
            edgeMode,
            Vector4.Zero);
        Vector4 blue = Sample(
            source,
            width,
            height,
            MapLensCorrection(plan, uv, width, height, blueShift),
            edgeMode,
            Vector4.Zero);
        return ApplyLensVignette(
            plan,
            uv,
            width,
            height,
            new Vector4(
                red.X,
                green.Y,
                blue.Z,
                MathF.Max(red.W, MathF.Max(green.W, blue.W))));
    }

    private static Vector2 MapLensCorrection(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        float chromaticShift)
    {
        float aspect = width / (float)height;
        Vector2 centered =
            (uv - new Vector2(0.5f)) *
            new Vector2(aspect, 1);
        centered = Rotate(centered, -plan.Options1.W);
        centered /= SafeLensScale(plan.Options2.X);
        centered = TiltLensCoordinate(
            centered,
            plan.Options1.Y,
            plan.Options1.Z);
        float radiusSquared = centered.LengthSquared();
        float radial = 1 +
            (Math.Clamp(
                plan.Options0.X + chromaticShift,
                -4,
                4) * radiusSquared);
        centered *= radial;
        return new Vector2(
            (centered.X / aspect) + 0.5f,
            centered.Y + 0.5f);
    }

    private static Vector4 ApplyLensVignette(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        Vector4 sampled)
    {
        float amount = Math.Clamp(plan.Options0.W, -4, 4);
        if (amount == 0)
        {
            return sampled;
        }

        float aspect = width / (float)height;
        Vector2 centered =
            (uv - new Vector2(0.5f)) *
            new Vector2(aspect, 1);
        float cornerRadius = MathF.Sqrt(
            ((aspect * aspect) + 1) * 0.25f);
        float radius = Math.Clamp(
            centered.Length() / cornerRadius,
            0,
            1);
        float midpoint = Math.Clamp(plan.Options1.X, 0, 1);
        float edge = SmoothStep(
            midpoint,
            1,
            radius);
        float factor = MathF.Max(0, 1 - (amount * edge));
        return new Vector4(
            sampled.X * factor,
            sampled.Y * factor,
            sampled.Z * factor,
            sampled.W);
    }

    private static Vector2 TiltLensCoordinate(
        Vector2 coordinate,
        float vertical,
        float horizontal)
    {
        float clampedHorizontal = Math.Clamp(horizontal, -64, 64);
        float horizontalScale = 1 /
            MathF.Sqrt(1 +
                (clampedHorizontal * clampedHorizontal));
        float x = (coordinate.X + clampedHorizontal) *
            horizontalScale;
        float z = (1 -
            (clampedHorizontal * coordinate.X)) *
            horizontalScale;
        float clampedVertical = Math.Clamp(vertical, -64, 64);
        float verticalScale = 1 /
            MathF.Sqrt(1 +
                (clampedVertical * clampedVertical));
        float y = (coordinate.Y -
            (clampedVertical * z)) * verticalScale;
        z = ((clampedVertical * coordinate.Y) + z) *
            verticalScale;
        float safeZ = MathF.Abs(z) < 0.000001f
            ? MathF.CopySign(0.000001f, z)
            : z;
        return new Vector2(x / safeZ, y / safeZ);
    }

    private static float SafeLensScale(float value) =>
        MathF.Abs(value) < 0.0001f
            ? MathF.CopySign(0.0001f, value)
            : value;

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

    internal static Vector2 MapDisplace(
        PrismResamplingPlan plan,
        Vector2 uv,
        Vector4 map,
        int width,
        int height)
    {
        Vector2 displacement = Vector2.Clamp(
            new Vector2(
                Channel(map, (int)plan.Options1.X),
                Channel(map, (int)plan.Options1.Y)),
            Vector2.Zero,
            Vector2.One) -
            new Vector2(0.5f);
        return uv -
            new Vector2(
                displacement.X *
                    plan.Options0.X / width,
                displacement.Y *
                    plan.Options0.Y / height);
    }

    internal static Vector2 MapDisplaceResourceCoordinate(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        Vector2 primaryResourceSize)
    {
        if (plan.Options0.Z < 0.5f)
        {
            return uv;
        }

        Vector2 mapUv = uv * new Vector2(
            width / primaryResourceSize.X,
            height / primaryResourceSize.Y);
        return Fract(mapUv);
    }

    internal static Vector2 MapGlass(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width,
        int height,
        Func<Vector2, Vector4>? primaryResource)
    {
        float radius =
            1 + (Math.Clamp(plan.Options0.Y, 0, 1) * 3);
        Vector2 position = new(x + 0.5f, y + 0.5f);
        float left = GlassHeight(
            plan,
            position - new Vector2(radius, 0),
            width,
            height,
            primaryResource);
        float right = GlassHeight(
            plan,
            position + new Vector2(radius, 0),
            width,
            height,
            primaryResource);
        float top = GlassHeight(
            plan,
            position - new Vector2(0, radius),
            width,
            height,
            primaryResource);
        float bottom = GlassHeight(
            plan,
            position + new Vector2(0, radius),
            width,
            height,
            primaryResource);
        Vector2 displacement = Vector2.Clamp(
            new Vector2(
                right - left,
                bottom - top) * 0.5f,
            new Vector2(-0.5f),
            new Vector2(0.5f));
        if (plan.Options1.X > 0.5f)
        {
            displacement = -displacement;
        }
        return uv -
            new Vector2(
                displacement.X * plan.Options0.X / width,
                displacement.Y * plan.Options0.X / height);
    }

    private static float GlassHeight(
        PrismResamplingPlan plan,
        Vector2 pixelPosition,
        int width,
        int height,
        Func<Vector2, Vector4>? primaryResource)
    {
        int texture = (int)plan.Options0.Z;
        float scaling = MathF.Max(
            MathF.Abs(plan.Options0.W),
            0.05f);
        if (texture == 4)
        {
            if (primaryResource is null)
            {
                return 0.5f;
            }

            Vector2 uv =
                ((pixelPosition / new Vector2(width, height)) -
                    new Vector2(0.5f)) /
                scaling +
                new Vector2(0.5f);
            Vector4 sample = primaryResource(uv);
            return Math.Clamp(
                Vector3.Dot(
                    new Vector3(
                        sample.X,
                        sample.Y,
                        sample.Z),
                    new Vector3(
                        0.2126f,
                        0.7152f,
                        0.0722f)),
                0,
                1);
        }

        float featureSize = texture switch
        {
            1 => 5,
            2 => 18,
            3 => 8,
            _ => 7
        };
        Vector2 coordinate =
            pixelPosition / (featureSize * scaling);
        Vector2 local =
            Fract(coordinate) - new Vector2(0.5f);
        return texture switch
        {
            1 => MathF.Sqrt(
                Math.Clamp(
                    1 - (4 * local.LengthSquared()),
                    0,
                    1)),
            2 => Math.Clamp(
                1 -
                    (2 * MathF.Max(
                        MathF.Abs(local.X),
                        MathF.Abs(local.Y))),
                0,
                1),
            3 => Math.Clamp(
                0.5f +
                    (0.25f *
                        MathF.Sin(
                            coordinate.X * MathF.Tau)) +
                    (0.25f *
                        MathF.Sin(
                            coordinate.Y * MathF.Tau)),
                0,
                1),
            _ => GlassValueNoise(coordinate)
        };
    }

    private static float GlassValueNoise(Vector2 coordinate)
    {
        int x = (int)MathF.Floor(coordinate.X);
        int y = (int)MathF.Floor(coordinate.Y);
        Vector2 fraction = Fract(coordinate);
        Vector2 blend =
            fraction * fraction *
            (new Vector2(3) - (2 * fraction));
        float top = float.Lerp(
            Hash(x, y, 0x6a09e667u),
            Hash(x + 1, y, 0x6a09e667u),
            blend.X);
        float bottom = float.Lerp(
            Hash(x, y + 1, 0x6a09e667u),
            Hash(x + 1, y + 1, 0x6a09e667u),
            blend.X);
        return float.Lerp(top, bottom, blend.Y);
    }

    internal static Vector2 MapOceanRipple(
        PrismResamplingPlan plan,
        Vector2 uv,
        int x,
        int y,
        int width,
        int height)
    {
        uint seed = Seed(plan.Options0.Z, plan.Options0.W);
        float size = MathF.Max(plan.Options0.X, 1);
        Vector2 position = new(x / size, y / size);
        Vector2 firstOctave = OceanWarpVector(position, seed);
        Vector2 warpedPosition =
            (position + (firstOctave * 0.75f)) * 2;
        Vector2 secondOctave = OceanWarpVector(
            warpedPosition,
            seed ^ 0x85ebca6bu);
        Vector2 displacement =
            (firstOctave + (secondOctave * 0.5f)) /
            1.5f;
        return uv + new Vector2(
            displacement.X * plan.Options0.Y / width,
            displacement.Y * plan.Options0.Y / height);
    }

    private static Vector2 OceanWarpVector(
        Vector2 position,
        uint seed) =>
        new(
            OceanSimplex(position, seed),
            OceanSimplex(
                new Vector2(
                    position.Y + 19.19f,
                    -position.X + 7.73f),
                seed ^ 0x9e3779b9u));

    private static float OceanSimplex(
        Vector2 position,
        uint seed)
    {
        const float skew = 0.3660254037844386f;
        const float unskew = 0.2113248654051871f;
        float skewed = (position.X + position.Y) * skew;
        int cellX = (int)MathF.Floor(position.X + skewed);
        int cellY = (int)MathF.Floor(position.Y + skewed);
        float cellOrigin = (cellX + cellY) * unskew;
        Vector2 first = position -
            new Vector2(cellX - cellOrigin, cellY - cellOrigin);
        int middleX = first.X > first.Y ? 1 : 0;
        int middleY = first.X > first.Y ? 0 : 1;
        Vector2 middle =
            first - new Vector2(middleX, middleY) +
            new Vector2(unskew);
        Vector2 last =
            first - Vector2.One + new Vector2(2 * unskew);

        return 70 * (
            OceanSimplexCorner(first, cellX, cellY, seed) +
            OceanSimplexCorner(
                middle,
                cellX + middleX,
                cellY + middleY,
                seed) +
            OceanSimplexCorner(
                last,
                cellX + 1,
                cellY + 1,
                seed));
    }

    private static float OceanSimplexCorner(
        Vector2 offset,
        int cellX,
        int cellY,
        uint seed)
    {
        float attenuation =
            0.5f - Vector2.Dot(offset, offset);
        if (attenuation <= 0)
        {
            return 0;
        }

        Vector2 gradient = OceanGradient(
            OceanHash(cellX, cellY, seed));
        attenuation *= attenuation;
        return attenuation * attenuation *
            Vector2.Dot(gradient, offset);
    }

    private static Vector2 OceanGradient(uint hash) =>
        (hash & 7) switch
        {
            0 => new Vector2(1, 0),
            1 => new Vector2(-1, 0),
            2 => new Vector2(0, 1),
            3 => new Vector2(0, -1),
            4 => new Vector2(0.70710678f, 0.70710678f),
            5 => new Vector2(-0.70710678f, 0.70710678f),
            6 => new Vector2(0.70710678f, -0.70710678f),
            _ => new Vector2(-0.70710678f, -0.70710678f)
        };

    private static uint OceanHash(
        int x,
        int y,
        uint seed)
    {
        uint hash = unchecked(
            ((uint)x * 0x8da6b343u) ^
            ((uint)y * 0xd8163841u) ^
            seed);
        hash ^= hash >> 16;
        hash = unchecked(hash * 0x7feb352du);
        hash ^= hash >> 15;
        hash = unchecked(hash * 0x846ca68bu);
        return hash ^ (hash >> 16);
    }

    internal static Vector2 MapPinch(
        Vector4 options,
        Vector2 uv)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 delta = uv - center;
        float radius = delta.Length() * 2;
        if (radius == 0 || radius >= 1)
        {
            return uv;
        }

        float amount =
            0.95f *
            (options.X / (1 + MathF.Abs(options.X)));
        float sineRadius = MathF.Sin(
            MathF.PI * 0.5f * radius);
        float factor = MathF.Pow(
            MathF.Max(sineRadius, 1e-20f),
            -amount);
        return center + (delta * factor);
    }

    internal static Vector2 MapPolar(
        Vector4 options,
        Vector2 uv,
        int width,
        int height)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 sourceSize = new(width, height);
        Vector2 centerPixels = center * sourceSize;
        Vector2 cornerDistance = new(
            MathF.Max(
                centerPixels.X,
                width - centerPixels.X),
            MathF.Max(
                centerPixels.Y,
                height - centerPixels.Y));
        float maximumRadius = MathF.Max(
            cornerDistance.Length(),
            0.000001f);
        if (options.X < 0.5f)
        {
            float angle =
                (uv.X - center.X) * MathF.Tau;
            float polarRadius =
                (uv.Y - center.Y + 0.5f) *
                maximumRadius;
            Vector2 mappedPixels =
                centerPixels + new Vector2(
                    MathF.Cos(angle),
                    MathF.Sin(angle)) * polarRadius;
            return mappedPixels / sourceSize;
        }

        Vector2 deltaPixels =
            (uv - center) * sourceSize;
        return new Vector2(
            center.X +
                (MathF.Atan2(
                    deltaPixels.Y,
                    deltaPixels.X) /
                    MathF.Tau),
            center.Y - 0.5f +
                (deltaPixels.Length() / maximumRadius));
    }

    internal static Vector2 MapRipple(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height)
    {
        uint seed = Seed(plan.Options0.Z, plan.Options0.W);
        float wavelength = MathF.Max(plan.Options0.Y, 1);
        float pixelY = uv.Y * height;
        float basePhase =
            MathF.Tau * pixelY / wavelength;
        float phaseNoise = OceanSimplex(
            new Vector2(
                pixelY / (wavelength * 4),
                0),
            seed);
        float seededPhase =
            ((seed & 0xffffu) / 65536f) *
            MathF.Tau;
        float phase =
            seededPhase + (phaseNoise * 1.1f);
        float displacement =
            (0.75f * MathF.Sin(basePhase + phase)) +
            (0.25f * MathF.Sin(
                (basePhase * 2.03f) +
                (phase * 0.55f)));
        return uv + new Vector2(
            displacement * plan.Options0.X / width,
            0);
    }

    internal static Vector2 MapShear(
        Vector4 options,
        Vector2 uv)
    {
        float y = Math.Clamp(uv.Y, 0, 1);
        (float startSlope, float middleSlope, float endSlope) =
            (int)options.Y switch
            {
                0 => (1, 1, 1),
                1 => (0, 1, 2),
                2 => (2, 1, 0),
                3 => (0, 1, 0),
                _ => (0, 2, 0)
            };
        float curve = y <= 0.5f
            ? Hermite(
                0,
                0.5f,
                startSlope * 0.5f,
                middleSlope * 0.5f,
                y * 2)
            : Hermite(
                0.5f,
                1,
                middleSlope * 0.5f,
                endSlope * 0.5f,
                (y - 0.5f) * 2);
        return uv - new Vector2(
            options.X * (curve - 0.5f),
            0);
    }

    private static float Hermite(
        float start,
        float end,
        float startTangent,
        float endTangent,
        float position)
    {
        float position2 = position * position;
        float position3 = position2 * position;
        float startBasis =
            (2 * position3) - (3 * position2) + 1;
        float startTangentBasis =
            position3 - (2 * position2) + position;
        float endBasis =
            (-2 * position3) + (3 * position2);
        float endTangentBasis =
            position3 - position2;
        return
            (startBasis * start) +
            (startTangentBasis * startTangent) +
            (endBasis * end) +
            (endTangentBasis * endTangent);
    }

    internal static Vector2 MapSpherizeCoordinate(
        Vector4 options,
        Vector2 uv)
    {
        Vector2 center = new(options.Z, options.W);
        Vector2 delta = uv - center;
        Vector2 normalized = delta * 2;
        if (options.Y is > 0.5f and < 1.5f)
        {
            normalized.Y = 0;
        }
        else if (options.Y > 1.5f)
        {
            normalized.X = 0;
        }

        float radius = normalized.Length();
        float amount = Math.Clamp(options.X, -1, 1);
        if (radius <= 0.000001f ||
            radius >= 1 ||
            amount == 0)
        {
            return uv;
        }


        float mappedRadius = amount > 0
            ? float.Lerp(
                radius,
                MathF.Asin(radius) * (2 / MathF.PI),
                amount)
            : float.Lerp(
                radius,
                MathF.Sin(radius * (MathF.PI / 2)),
                -amount);
        float scale = mappedRadius / radius;
        Vector2 warped = delta;
        if (options.Y is > 0.5f and < 1.5f)
        {
            warped.X *= scale;
        }
        else if (options.Y > 1.5f)
        {
            warped.Y *= scale;
        }
        else
        {
            warped *= scale;
        }
        return center + warped;
    }

    internal static Vector2 MapTwirl(
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

    internal static int TwirlTapCount(
        Vector4 options,
        Vector2 uv,
        int width,
        int height)
    {
        TwirlJacobian(
            options,
            uv,
            width,
            height,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        return FelineTapCount(
            MajorFootprint(
                derivativeX,
                derivativeY).Length);
    }

    internal static Vector4 SampleTwirlFeline(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped,
        int edgeMode,
        Vector4 fill)
    {
        TwirlJacobian(
            plan.Options0,
            uv,
            width,
            height,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        return SampleFeline(
            source,
            width,
            height,
            mapped,
            derivativeX,
            derivativeY,
            edgeMode,
            fill);
    }

    private static Vector4 SampleFeline(
        Vector4[] source,
        int width,
        int height,
        Vector2 mapped,
        Vector2 derivativeX,
        Vector2 derivativeY,
        int edgeMode,
        Vector4 fill)
    {
        MajorAxis footprint =
            MajorFootprint(
                derivativeX,
                derivativeY);
        int tapCount = FelineTapCount(footprint.Length);
        if (tapCount == 1)
        {
            return Sample(
                source,
                width,
                height,
                mapped,
                edgeMode,
                fill);
        }

        float boundedLength = MathF.Min(
            footprint.Length,
            8);
        Vector2 axis =
            footprint.Direction *
            boundedLength /
            new Vector2(width, height);
        Vector4 total = Vector4.Zero;
        float totalWeight = 0;
        for (int tap = 0; tap < tapCount; tap++)
        {
            float position =
                ((tap + 0.5f) / tapCount) -
                0.5f;
            float weight = MathF.Exp(
                -2 * position * position);
            total += Sample(
                source,
                width,
                height,
                mapped + (axis * position),
                edgeMode,
                fill) * weight;
            totalWeight += weight;
        }
        return total / totalWeight;
    }

    private static void TwirlJacobian(
        Vector4 options,
        Vector2 uv,
        int width,
        int height,
        out Vector2 derivativeX,
        out Vector2 derivativeY)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 delta = uv - center;
        float deltaLength = delta.Length();
        float radius = deltaLength / 0.70710678f;
        float twist =
            -options.X *
            Math.Clamp(1 - radius, 0, 1);
        Vector2 sourceSize = new(width, height);
        if (radius >= 1)
        {
            derivativeX = Vector2.UnitX;
            derivativeY = Vector2.UnitY;
            return;
        }

        Vector2 rotatedDelta = Rotate(delta, twist);
        Vector2 tangent = new(
            -rotatedDelta.Y,
            rotatedDelta.X);
        Vector2 radialDirection = deltaLength > 0.000001f
            ? delta / deltaLength
            : Vector2.Zero;
        Vector2 angleGradient =
            radialDirection *
            (options.X / 0.70710678f);
        Vector2 stepX = new(1f / width, 0);
        Vector2 stepY = new(0, 1f / height);
        derivativeX =
            (Rotate(stepX, twist) +
                (tangent *
                    Vector2.Dot(angleGradient, stepX))) *
            sourceSize;
        derivativeY =
            (Rotate(stepY, twist) +
                (tangent *
                    Vector2.Dot(angleGradient, stepY))) *
            sourceSize;
    }

    private static MajorAxis MajorFootprint(
        Vector2 derivativeX,
        Vector2 derivativeY)
    {
        double covarianceX =
            ((double)derivativeX.X * derivativeX.X) +
            ((double)derivativeY.X * derivativeY.X);
        double covarianceY =
            ((double)derivativeX.Y * derivativeX.Y) +
            ((double)derivativeY.Y * derivativeY.Y);
        double covarianceCross =
            ((double)derivativeX.X * derivativeX.Y) +
            ((double)derivativeY.X * derivativeY.Y);
        double difference = covarianceX - covarianceY;
        double discriminant = Math.Sqrt(Math.Max(
            (difference * difference) +
                (4 * covarianceCross * covarianceCross),
            0));
        double majorEigenvalue = Math.Max(
            (covarianceX + covarianceY + discriminant) * 0.5,
            0);
        double majorLength = Math.Sqrt(majorEigenvalue);
        Vector2 direction;
        if (Math.Abs(covarianceCross) > 0.000001)
        {
            double directionX = covarianceCross;
            double directionY =
                majorEigenvalue - covarianceX;
            double directionLength = Math.Sqrt(
                (directionX * directionX) +
                (directionY * directionY));
            direction = directionLength > 0
                ? new Vector2(
                    (float)(directionX / directionLength),
                    (float)(directionY / directionLength))
                : Vector2.UnitX;
        }
        else
        {
            direction = covarianceX >= covarianceY
                ? Vector2.UnitX
                : Vector2.UnitY;
        }
        return new MajorAxis(
            direction,
            (float)Math.Min(majorLength, float.MaxValue));
    }

    private static int FelineTapCount(float majorLength) =>
        majorLength <= 1
            ? 1
            : majorLength <= 4
                ? 4
                : 8;

    internal static Vector2 MapWave(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height)
    {
        WaveJacobian(
            plan,
            uv,
            width,
            height,
            out Vector2 mapped,
            out _,
            out _);
        return mapped;
    }

    internal static Vector4 SampleWaveFeline(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        int edgeMode,
        Vector4 fill)
    {
        WaveJacobian(
            plan,
            uv,
            width,
            height,
            out Vector2 mapped,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        return SampleFeline(
            source,
            width,
            height,
            mapped,
            derivativeX,
            derivativeY,
            edgeMode,
            fill);
    }

    private static void WaveJacobian(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        out Vector2 mapped,
        out Vector2 derivativeX,
        out Vector2 derivativeY)
    {
        uint seed = Seed(
            plan.Options2.Y,
            plan.Options2.Z);
        int generators = Math.Clamp(
            (int)MathF.Round(plan.Options0.X),
            1,
            PrismResamplingPlanner.MaximumWaveGenerators);
        int kind = (int)plan.Options0.W;
        Vector2 displacement = Vector2.Zero;
        Vector2 displacementDerivativeX = Vector2.Zero;
        Vector2 displacementDerivativeY = Vector2.Zero;
        Vector2 pixelPosition = uv * new Vector2(
            width,
            height);
        for (int generator = 0;
            generator < generators;
            generator++)
        {
            float directionAngle =
                WaveHash(seed, generator, 0) *
                MathF.Tau;
            Vector2 direction = new(
                MathF.Cos(directionAngle),
                MathF.Sin(directionAngle));
            float wavelength = float.Lerp(
                plan.Options0.Y,
                plan.Options0.Z,
                WaveHash(seed, generator, 1));
            float amplitude = float.Lerp(
                plan.Options1.X,
                plan.Options1.Y,
                WaveHash(seed, generator, 2));
            WaveSample wave = BandLimitedWave(
                (Vector2.Dot(
                    pixelPosition,
                    direction) /
                    wavelength) +
                    WaveHash(seed, generator, 3),
                direction / wavelength,
                kind);
            Vector2 displacementDirection =
                direction * amplitude;
            displacement +=
                displacementDirection *
                wave.Value;
            displacementDerivativeX +=
                displacementDirection *
                wave.Derivative *
                direction.X /
                wavelength;
            displacementDerivativeY +=
                displacementDirection *
                wave.Derivative *
                direction.Y /
                wavelength;
        }

        float normalization =
            1 / MathF.Sqrt(generators);
        float scaleX = plan.Options1.Z * normalization;
        float scaleY = plan.Options1.W * normalization;
        mapped = uv + new Vector2(
            displacement.X * scaleX / width,
            displacement.Y * scaleY / height);
        derivativeX = new Vector2(
            1 + (displacementDerivativeX.X * scaleX),
            displacementDerivativeX.Y * scaleY);
        derivativeY = new Vector2(
            displacementDerivativeY.X * scaleX,
            1 + (displacementDerivativeY.Y * scaleY));
    }

    internal static Vector2 MapZigZag(
        Vector4 options0,
        Vector4 options1,
        Vector2 uv,
        int width,
        int height)
    {
        Vector2 size = new(width, height);
        Vector2 center = new(
            options1.X,
            options1.Y);
        Vector2 centerPixels = center * size;
        Vector2 deltaPixels =
            (uv - center) * size;
        float radius = deltaPixels.Length();
        if (radius < 0.000001f)
        {
            return uv;
        }

        Vector2 cornerDistance = new(
            MathF.Max(
                MathF.Abs(centerPixels.X),
                MathF.Abs(width - centerPixels.X)),
            MathF.Max(
                MathF.Abs(centerPixels.Y),
                MathF.Abs(height - centerPixels.Y)));
        float maximumRadius = MathF.Max(
            cornerDistance.Length(),
            0.000001f);
        float normalizedRadius = Math.Clamp(
            radius / maximumRadius,
            0,
            1);
        float ridges = Math.Clamp(
            options0.Y,
            1,
            MathF.Max(maximumRadius, 1));
        float strength = Math.Clamp(
            options0.X,
            -1,
            1);
        float envelope = MathF.Sin(
            MathF.PI * normalizedRadius);
        float oscillation = MathF.Cos(
            MathF.PI *
            ridges *
            normalizedRadius);

        const float maximumSlope = 0.85f;
        float maximumDisplacement =
            maximumRadius *
            maximumSlope /
            (MathF.PI * (ridges + 1));
        float displacement =
            strength *
            maximumDisplacement *
            envelope *
            oscillation;

        Vector2 mappedPixels;
        if (options0.Z < 0.5f)
        {
            mappedPixels =
                (uv * size) +
                (Vector2.Normalize(Vector2.One) *
                    displacement);
        }
        else if (options0.Z < 1.5f)
        {
            mappedPixels =
                centerPixels +
                (deltaPixels *
                    ((radius + displacement) / radius));
        }
        else
        {
            mappedPixels =
                centerPixels +
                Rotate(
                    deltaPixels,
                    displacement / radius);
        }
        return mappedPixels / size;
    }

    internal static Vector2 MapLiquify(
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

    internal static Vector4 SampleLiquify(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped,
        int edgeMode,
        Vector4 fill,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector4 bilinear = Sample(
            source,
            width,
            height,
            mapped,
            edgeMode,
            fill);
        float cubicConfidence = LiquifyCubicConfidence(
            plan,
            uv,
            width,
            height,
            primaryResource,
            auxiliaryResource);
        if (cubicConfidence <= 0.001f)
        {
            return bilinear;
        }

        Vector4 bicubic = SampleBicubic(
            source,
            width,
            height,
            mapped,
            edgeMode,
            fill);
        return cubicConfidence >= 0.999f
            ? bicubic
            : Vector4.Lerp(
                bilinear,
                bicubic,
                cubicConfidence);
    }

    private static float LiquifyCubicConfidence(
        PrismResamplingPlan plan,
        Vector2 uv,
        int width,
        int height,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource)
    {
        Vector2 pixelSize = new(
            1f / width,
            1f / height);
        Vector2 leftUv = Vector2.Clamp(
            uv - new Vector2(pixelSize.X, 0),
            Vector2.Zero,
            Vector2.One);
        Vector2 rightUv = Vector2.Clamp(
            uv + new Vector2(pixelSize.X, 0),
            Vector2.Zero,
            Vector2.One);
        Vector2 topUv = Vector2.Clamp(
            uv - new Vector2(0, pixelSize.Y),
            Vector2.Zero,
            Vector2.One);
        Vector2 bottomUv = Vector2.Clamp(
            uv + new Vector2(0, pixelSize.Y),
            Vector2.Zero,
            Vector2.One);
        Vector2 sourceSize = new(width, height);
        Vector2 derivativeX =
            (MapLiquifyResource(
                plan,
                rightUv,
                primaryResource,
                auxiliaryResource) -
            MapLiquifyResource(
                plan,
                leftUv,
                primaryResource,
                auxiliaryResource)) *
            sourceSize /
            MathF.Max(
                (rightUv.X - leftUv.X) * width,
                0.000001f);
        Vector2 derivativeY =
            (MapLiquifyResource(
                plan,
                bottomUv,
                primaryResource,
                auxiliaryResource) -
            MapLiquifyResource(
                plan,
                topUv,
                primaryResource,
                auxiliaryResource)) *
            sourceSize /
            MathF.Max(
                (bottomUv.Y - topUv.Y) * height,
                0.000001f);
        if (!float.IsFinite(derivativeX.X) ||
            !float.IsFinite(derivativeX.Y) ||
            !float.IsFinite(derivativeY.X) ||
            !float.IsFinite(derivativeY.Y))
        {
            return 0;
        }

        float determinant =
            (derivativeX.X * derivativeY.Y) -
            (derivativeX.Y * derivativeY.X);
        float maximumAxis = MathF.Max(
            derivativeX.Length(),
            derivativeY.Length());
        float orientationConfidence = SmoothStep(
            0.05f,
            0.25f,
            determinant);
        float footprintConfidence =
            1 -
            SmoothStep(
                2,
                4,
                maximumAxis);
        return Math.Clamp(
            orientationConfidence * footprintConfidence,
            0,
            1);
    }

    private static Vector2 MapLiquifyResource(
        PrismResamplingPlan plan,
        Vector2 uv,
        Func<Vector2, Vector4>? primaryResource,
        Func<Vector2, Vector4>? auxiliaryResource) =>
        MapLiquify(
            plan,
            uv,
            primaryResource?.Invoke(uv) ??
                new Vector4(0.5f, 0.5f, 0, 1),
            auxiliaryResource?.Invoke(uv));

    internal static Vector4 BloomHorizontal(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        return GaussianAxis(
            plan,
            source,
            width,
            height,
            x,
            y,
            plan.Options0.W,
            horizontal: true,
            brightPass: true);
    }

    internal static Vector4 BloomVerticalComposite(
        PrismResamplingPlan plan,
        Vector4[] source,
        Vector4[] original,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 bloom = GaussianAxis(
            plan,
            source,
            width,
            height,
            x,
            y,
            plan.Options0.W,
            horizontal: false,
            brightPass: false);
        Vector4 basePixel = original[(y * width) + x];
        float strength =
            Math.Clamp(plan.Options0.Y, 0, 1) *
            Math.Clamp(plan.Options1.W, 0, 1);
        Vector3 contribution = new(
            bloom.X * plan.Options1.X * strength,
            bloom.Y * plan.Options1.Y * strength,
            bloom.Z * plan.Options1.Z * strength);
        Vector3 combined = Vector3.Min(
            new Vector3(basePixel.W),
            new Vector3(
                basePixel.X,
                basePixel.Y,
                basePixel.Z) + contribution);
        return new Vector4(combined, basePixel.W);
    }

    internal static Vector4 GaussianAxis(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius,
        bool horizontal,
        bool brightPass)
    {
        radius = MathF.Max(radius, 0.5f);
        Vector4 total = Vector4.Zero;
        total += BloomSample(0, 0.38774f);
        total += BloomSample(-radius, 0.24477f);
        total += BloomSample(radius, 0.24477f);
        total += BloomSample(-radius * 2, 0.06136f);
        total += BloomSample(radius * 2, 0.06136f);
        return total;

        Vector4 BloomSample(float offset, float weight)
        {
            Vector4 sample = SamplePixel(
                source,
                width,
                height,
                horizontal ? x + offset : x,
                horizontal ? y : y + offset,
                0);
            if (brightPass &&
                Vector3.Dot(
                    Unpremultiply(sample),
                    new Vector3(0.2126f, 0.7152f, 0.0722f)) <
                plan.Options0.Z)
            {
                sample = Vector4.Zero;
            }
            return sample * weight;
        }
    }

    internal static Vector4 NeonGlowEdge(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float topLeft = Signal(-1, -1);
        float top = Signal(0, -1);
        float topRight = Signal(1, -1);
        float left = Signal(-1, 0);
        float right = Signal(1, 0);
        float bottomLeft = Signal(-1, 1);
        float bottom = Signal(0, 1);
        float bottomRight = Signal(1, 1);
        float gradientX =
            -topLeft + topRight -
            (2 * left) + (2 * right) -
            bottomLeft + bottomRight;
        float gradientY =
            -topLeft - (2 * top) - topRight +
            bottomLeft + (2 * bottom) + bottomRight;
        float edge = Math.Clamp(
            MathF.Sqrt(
                (gradientX * gradientX) +
                (gradientY * gradientY)) / 4,
            0,
            1);
        return new Vector4(edge);

        float Signal(int offsetX, int offsetY)
        {
            Vector4 sample = SamplePixel(
                source,
                width,
                height,
                x + offsetX,
                y + offsetY,
                0);
            float luminance = Vector3.Dot(
                new Vector3(sample.X, sample.Y, sample.Z),
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            return MathF.Max(
                luminance,
                sample.W * 0.25f);
        }
    }

    internal static Vector4 NeonGlowPyramidComposite(
        PrismResamplingPlan plan,
        Vector4 original,
        MipLevel[] mipChain,
        Vector2 uv)
    {
        float maximumLod = Math.Clamp(
            plan.Options0.Z,
            0,
            mipChain.Length - 1);
        float mask =
            (Mask(0) * 0.32f) +
            (Mask(maximumLod * 0.25f) * 0.25f) +
            (Mask(maximumLod * 0.5f) * 0.19f) +
            (Mask(maximumLod * 0.75f) * 0.14f) +
            (Mask(maximumLod) * 0.10f);
        float strength =
            Math.Clamp(plan.Options0.W, 0, 8) *
            Math.Clamp(plan.Options1.W, 0, 1) *
            mask;
        Vector3 contribution = new(
            plan.Options1.X * strength,
            plan.Options1.Y * strength,
            plan.Options1.Z * strength);
        Vector3 combined = Vector3.Min(
            new Vector3(original.W),
            new Vector3(
                original.X,
                original.Y,
                original.Z) + contribution);
        return new Vector4(combined, original.W);

        float Mask(float lod) =>
            SampleMipmapped(
                mipChain,
                uv,
                lod,
                0,
                Vector4.Zero).X;
    }

    internal static Vector4 Grain(
        PrismResamplingPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        float noise = Hash(x, y, 9173) - 0.5f;
        Vector3 straight = Vector3.Clamp(
            Unpremultiply(center) +
                new Vector3(
                    noise * Math.Clamp(
                        plan.Options0.X,
                        0,
                        1)),
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

    private static Vector4 SampleBicubic(
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

        float sampleX = (uv.X * width) - 0.5f;
        float sampleY = (uv.Y * height) - 0.5f;
        int baseX = (int)MathF.Floor(sampleX);
        int baseY = (int)MathF.Floor(sampleY);
        float fractionX = sampleX - baseX;
        float fractionY = sampleY - baseY;
        int tapEdgeMode = edgeMode is 2 or 3
            ? edgeMode
            : 0;
        Vector4 total = Vector4.Zero;
        for (int offsetY = -1; offsetY <= 2; offsetY++)
        {
            float weightY = CubicWeight(
                offsetY - fractionY);
            Vector4 row = Vector4.Zero;
            for (int offsetX = -1; offsetX <= 2; offsetX++)
            {
                float weightX = CubicWeight(
                    offsetX - fractionX);
                row += SamplePixel(
                        source,
                        width,
                        height,
                        baseX + offsetX,
                        baseY + offsetY,
                        tapEdgeMode) *
                    weightX;
            }
            total += row * weightY;
        }
        return total;
    }

    private static float CubicWeight(float distance)
    {
        const float coefficient = -0.75f;
        float absolute = MathF.Abs(distance);
        if (absolute <= 1)
        {
            return
                ((coefficient + 2) *
                    absolute *
                    absolute *
                    absolute) -
                ((coefficient + 3) *
                    absolute *
                    absolute) +
                1;
        }
        if (absolute < 2)
        {
            return
                (coefficient *
                    absolute *
                    absolute *
                    absolute) -
                (5 * coefficient *
                    absolute *
                    absolute) +
                (8 * coefficient * absolute) -
                (4 * coefficient);
        }
        return 0;
    }

    internal static Vector4 SampleTransform(
        PrismResamplingPlan plan,
        MipLevel[] mipChain,
        Vector2 uv,
        Vector2 mapped,
        int width,
        int height,
        int edgeMode,
        Vector4 fill)
    {
        Vector2 sourceSize = new(width, height);
        Vector2 derivativeX =
            (MapTransform(
                plan,
                uv + new Vector2(1f / width, 0)) -
            mapped) * sourceSize;
        Vector2 derivativeY =
            (MapTransform(
                plan,
                uv + new Vector2(0, 1f / height)) -
            mapped) * sourceSize;
        float lengthX = derivativeX.Length();
        float lengthY = derivativeY.Length();
        Vector2 majorDerivative = lengthX >= lengthY
            ? derivativeX
            : derivativeY;
        float major = MathF.Max(lengthX, lengthY);
        float minor = MathF.Max(
            1,
            MathF.Min(lengthX, lengthY));
        float lod = MathF.Max(0, MathF.Log2(minor));
        int tapCount = Math.Clamp(
            (int)MathF.Ceiling(major / minor),
            1,
            4);
        if (tapCount == 1 || major <= 0)
        {
            return SampleMipmapped(
                mipChain,
                mapped,
                lod,
                edgeMode,
                fill);
        }

        Vector4 total = Vector4.Zero;
        Vector2 span =
            majorDerivative /
            sourceSize;
        for (int tap = 0; tap < tapCount; tap++)
        {
            float position =
                ((tap + 0.5f) / tapCount) -
                0.5f;
            total += SampleMipmapped(
                mipChain,
                mapped + (span * position),
                lod,
                edgeMode,
                fill);
        }

        return total / tapCount;
    }

    internal static Vector4 SamplePolarEwa(
        PrismResamplingPlan plan,
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        Vector2 mapped)
    {
        Vector2 sourceSize = new(width, height);
        PolarJacobian(
            plan.Options0,
            uv,
            width,
            height,
            out Vector2 derivativeX,
            out Vector2 derivativeY);
        float covarianceX =
            (derivativeX.X * derivativeX.X) +
            (derivativeY.X * derivativeY.X);
        float covarianceY =
            (derivativeX.Y * derivativeX.Y) +
            (derivativeY.Y * derivativeY.Y);
        float covarianceCross =
            (derivativeX.X * derivativeX.Y) +
            (derivativeY.X * derivativeY.Y);
        float trace = covarianceX + covarianceY;
        float discriminant = MathF.Sqrt(MathF.Max(
            ((covarianceX - covarianceY) *
                (covarianceX - covarianceY)) +
            (4 * covarianceCross * covarianceCross),
            0));
        float majorEigenvalue =
            MathF.Max((trace + discriminant) * 0.5f, 0);
        float minorEigenvalue =
            MathF.Max((trace - discriminant) * 0.5f, 0);
        float majorLength = MathF.Sqrt(majorEigenvalue);
        float minorLength = MathF.Max(
            MathF.Sqrt(minorEigenvalue),
            1);
        if (majorLength <= 1)
        {
            return SamplePolarSource(
                source,
                width,
                height,
                mapped,
                plan.Options0.X >= 0.5f);
        }

        minorLength = MathF.Max(
            minorLength,
            majorLength / 8);
        Vector2 majorDirection;
        if (MathF.Abs(covarianceCross) > 0.000001f)
        {
            majorDirection = Vector2.Normalize(new Vector2(
                covarianceCross,
                majorEigenvalue - covarianceX));
        }
        else
        {
            majorDirection = covarianceX >= covarianceY
                ? Vector2.UnitX
                : Vector2.UnitY;
        }
        Vector2 minorDirection = new(
            -majorDirection.Y,
            majorDirection.X);
        Vector2 majorAxis =
            majorDirection * majorLength / sourceSize;
        Vector2 minorAxis =
            minorDirection * minorLength / sourceSize;
        const float innerRadius = 0.2f;
        const float outerComponent = 0.31819805f;
        const float innerWeight = 0.92311635f;
        const float outerWeight = 0.66697681f;
        const float totalWeight =
            4 * (innerWeight + outerWeight);
        bool wrapAngle = plan.Options0.X >= 0.5f;
        Vector4 total =
            SamplePolarSource(
                source,
                width,
                height,
                mapped + (majorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped - (majorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped + (minorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped - (minorAxis * innerRadius),
                wrapAngle) *
            innerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped +
                    ((majorAxis + minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped +
                    ((majorAxis - minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped +
                    ((-majorAxis + minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        total += SamplePolarSource(
                source,
                width,
                height,
                mapped -
                    ((majorAxis + minorAxis) * outerComponent),
                wrapAngle) *
            outerWeight;
        return total / totalWeight;
    }

    private static void PolarJacobian(
        Vector4 options,
        Vector2 uv,
        int width,
        int height,
        out Vector2 derivativeX,
        out Vector2 derivativeY)
    {
        Vector2 center = new(options.Y, options.Z);
        Vector2 sourceSize = new(width, height);
        Vector2 centerPixels = center * sourceSize;
        Vector2 cornerDistance = new(
            MathF.Max(
                centerPixels.X,
                width - centerPixels.X),
            MathF.Max(
                centerPixels.Y,
                height - centerPixels.Y));
        float maximumRadius = MathF.Max(
            cornerDistance.Length(),
            0.000001f);
        if (options.X < 0.5f)
        {
            float angle =
                (uv.X - center.X) * MathF.Tau;
            float polarRadius =
                (uv.Y - center.Y + 0.5f) *
                maximumRadius;
            Vector2 direction = new(
                MathF.Cos(angle),
                MathF.Sin(angle));
            Vector2 tangent = new(
                -direction.Y,
                direction.X);
            derivativeX =
                tangent * polarRadius * MathF.Tau / width;
            derivativeY =
                direction * maximumRadius / height;
            return;
        }

        Vector2 deltaPixels =
            (uv - center) * sourceSize;
        float radiusSquared = deltaPixels.LengthSquared();
        float radialScale = height / maximumRadius;
        if (radiusSquared < 0.000001f)
        {
            derivativeX = new Vector2(0, radialScale);
            derivativeY = new Vector2(
                width * 0.25f,
                radialScale);
            return;
        }

        float radius = MathF.Sqrt(radiusSquared);
        float angularScale =
            width / (MathF.Tau * radiusSquared);
        derivativeX = new Vector2(
            -deltaPixels.Y * angularScale,
            deltaPixels.X * radialScale / radius);
        derivativeY = new Vector2(
            deltaPixels.X * angularScale,
            deltaPixels.Y * radialScale / radius);
    }

    private static Vector4 SamplePolarSource(
        Vector4[] source,
        int width,
        int height,
        Vector2 uv,
        bool wrapAngle)
    {
        if (!wrapAngle)
        {
            return Sample(
                source,
                width,
                height,
                uv,
                1,
                Vector4.Zero);
        }
        if (uv.Y < 0 || uv.Y > 1)
        {
            return Vector4.Zero;
        }

        uv.X -= MathF.Floor(uv.X);
        float sampleX = (uv.X * width) - 0.5f;
        float sampleY = (uv.Y * height) - 0.5f;
        int x0 = (int)MathF.Floor(sampleX);
        int y0 = (int)MathF.Floor(sampleY);
        float fractionX = sampleX - x0;
        float fractionY = sampleY - y0;
        Vector4 top = Vector4.Lerp(
            SamplePolarPixel(
                source,
                width,
                height,
                x0,
                y0),
            SamplePolarPixel(
                source,
                width,
                height,
                x0 + 1,
                y0),
            fractionX);
        Vector4 bottom = Vector4.Lerp(
            SamplePolarPixel(
                source,
                width,
                height,
                x0,
                y0 + 1),
            SamplePolarPixel(
                source,
                width,
                height,
                x0 + 1,
                y0 + 1),
            fractionX);
        return Vector4.Lerp(top, bottom, fractionY);
    }

    private static Vector4 SamplePolarPixel(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        if (y < 0 || y >= height)
        {
            return Vector4.Zero;
        }
        return source[(y * width) + Wrap(x, width)];
    }

    private static Vector4 SampleMipmapped(
        MipLevel[] mipChain,
        Vector2 uv,
        float lod,
        int edgeMode,
        Vector4 fill)
    {
        int lowerLevel = Math.Clamp(
            (int)MathF.Floor(lod),
            0,
            mipChain.Length - 1);
        int upperLevel = Math.Min(
            lowerLevel + 1,
            mipChain.Length - 1);
        MipLevel lower = mipChain[lowerLevel];
        Vector4 lowerSample = Sample(
            lower.Pixels,
            lower.Width,
            lower.Height,
            uv,
            edgeMode,
            fill);
        if (lowerLevel == upperLevel)
        {
            return lowerSample;
        }

        MipLevel upper = mipChain[upperLevel];
        Vector4 upperSample = Sample(
            upper.Pixels,
            upper.Width,
            upper.Height,
            uv,
            edgeMode,
            fill);
        return Vector4.Lerp(
            lowerSample,
            upperSample,
            lod - lowerLevel);
    }

    private static MipLevel[] BuildMipChain(
        Vector4[] source,
        int width,
        int height)
    {
        List<MipLevel> levels =
        [
            new MipLevel(source, width, height)
        ];
        Vector4[] current = source;
        int currentWidth = width;
        int currentHeight = height;
        while (currentWidth > 1 || currentHeight > 1)
        {
            int nextWidth = Math.Max(1, currentWidth / 2);
            int nextHeight = Math.Max(1, currentHeight / 2);
            Vector4[] next = new Vector4[nextWidth * nextHeight];
            for (int y = 0; y < nextHeight; y++)
            {
                int sourceY = y * 2;
                int sourceY1 = Math.Min(
                    sourceY + 1,
                    currentHeight - 1);
                for (int x = 0; x < nextWidth; x++)
                {
                    int sourceX = x * 2;
                    int sourceX1 = Math.Min(
                        sourceX + 1,
                        currentWidth - 1);
                    next[(y * nextWidth) + x] =
                        (current[(sourceY * currentWidth) + sourceX] +
                        current[(sourceY * currentWidth) + sourceX1] +
                        current[(sourceY1 * currentWidth) + sourceX] +
                        current[(sourceY1 * currentWidth) + sourceX1]) /
                        4;
                }
            }

            levels.Add(new MipLevel(next, nextWidth, nextHeight));
            current = next;
            currentWidth = nextWidth;
            currentHeight = nextHeight;
        }

        return [.. levels];
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

    private static WaveSample BandLimitedWave(
        float phase,
        Vector2 phaseWidth,
        int kind)
    {
        float wrapped = phase - MathF.Floor(phase);
        float maximumWidth = MathF.Max(
            MathF.Abs(phaseWidth.X),
            MathF.Abs(phaseWidth.Y));
        if (kind == 0)
        {
            if (maximumWidth > 0.5f)
            {
                return default;
            }

            float attenuation =
                Sinc(MathF.PI * phaseWidth.X) *
                Sinc(MathF.PI * phaseWidth.Y);
            float angle = wrapped * MathF.Tau;
            return new WaveSample(
                MathF.Sin(angle) * attenuation,
                MathF.Tau *
                    MathF.Cos(angle) *
                    attenuation);
        }

        float value = 0;
        float derivative = 0;
        for (int term = 0; term < 8; term++)
        {
            int harmonic = (term * 2) + 1;
            float harmonicWidth =
                harmonic * maximumWidth;
            if (harmonicWidth > 0.5f)
            {
                break;
            }

            float attenuation =
                Sinc(
                    MathF.PI *
                    harmonic *
                    phaseWidth.X) *
                Sinc(
                    MathF.PI *
                    harmonic *
                    phaseWidth.Y);
            float angle =
                wrapped *
                harmonic *
                MathF.Tau;
            if (kind == 1)
            {
                float coefficient =
                    8 /
                    (MathF.PI *
                        MathF.PI *
                        harmonic *
                        harmonic);
                value +=
                    coefficient *
                    MathF.Cos(angle) *
                    attenuation;
                derivative -=
                    coefficient *
                    MathF.Tau *
                    harmonic *
                    MathF.Sin(angle) *
                    attenuation;
            }
            else
            {
                float coefficient =
                    -4 /
                    (MathF.PI * harmonic);
                value +=
                    coefficient *
                    MathF.Sin(angle) *
                    attenuation;
                derivative +=
                    coefficient *
                    MathF.Tau *
                    harmonic *
                    MathF.Cos(angle) *
                    attenuation;
            }
        }
        return new WaveSample(value, derivative);
    }

    private static float Sinc(float value) =>
        MathF.Abs(value) < 0.0001f
            ? 1
            : MathF.Sin(value) / value;

    private static float WaveHash(
        uint seed,
        int generator,
        int channel)
    {
        uint value =
            seed ^
            unchecked(
                ((uint)generator + 1) *
                0x9e3779b9u) ^
            unchecked(
                ((uint)channel + 1) *
                0x85ebca6bu);
        value ^= value >> 16;
        value = unchecked(value * 0x7feb352du);
        value ^= value >> 15;
        value = unchecked(value * 0x846ca68bu);
        value ^= value >> 16;
        return (value & 0x00ffffffu) /
            16777216f;
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

    internal readonly record struct MipLevel(
        Vector4[] Pixels,
        int Width,
        int Height);

    private readonly record struct MajorAxis(
        Vector2 Direction,
        float Length);

    private readonly record struct WaveSample(
        float Value,
        float Derivative);
}
