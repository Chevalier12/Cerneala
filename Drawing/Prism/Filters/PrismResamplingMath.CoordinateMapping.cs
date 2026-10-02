using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;

namespace Cerneala.Drawing.Prism.Filters;

internal static partial class PrismResamplingMath
{
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
            PrismCatalogFilterMath.Hash(x, y, 0x6a09e667u),
            PrismCatalogFilterMath.Hash(x + 1, y, 0x6a09e667u),
            blend.X);
        float bottom = float.Lerp(
            PrismCatalogFilterMath.Hash(x, y + 1, 0x6a09e667u),
            PrismCatalogFilterMath.Hash(x + 1, y + 1, 0x6a09e667u),
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

}
