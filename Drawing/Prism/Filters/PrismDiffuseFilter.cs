using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismDiffuseFilter
{
    private const float Epsilon = 0.000001f;
    private const int DarkenOnlyMode = -1067514420;
    private const int LightenOnlyMode = 1153015394;
    private const int AnisotropicMode = -813703062;

    public static Vector4 Apply(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = source[(y * width) + x];
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float stepX = MathF.Max(pass.RadiusX, Epsilon);
        float stepY = MathF.Max(pass.RadiusY, Epsilon);
        Vector3 tensor = Vector3.Zero;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            float weightY = offsetY == 0 ? 2 : 1;
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                float weightX = offsetX == 0 ? 2 : 1;
                AccumulateTensor(
                    source,
                    width,
                    height,
                    x + (offsetX * stepX),
                    y + (offsetY * stepY),
                    stepX,
                    stepY,
                    weightX * weightY,
                    ref tensor);
            }
        }

        float centerLuminance = Luminance(center);
        float gradientX = 0.5f * (
            Luminance(Sample(source, width, height, x + stepX, y)) -
            Luminance(Sample(source, width, height, x - stepX, y)));
        float gradientY = 0.5f * (
            Luminance(Sample(source, width, height, x, y + stepY)) -
            Luminance(Sample(source, width, height, x, y - stepY)));
        Vector2 localNormal = NormalizeOrZero(
            new Vector2(gradientX, gradientY));
        (Vector2 tensorNormal, float coherence) =
            PrincipalDirection(tensor);

        int mode = UnpackInteger(plan.GetOption("Mode"));
        bool anisotropic = mode == AnisotropicMode;
        Vector2 direction = anisotropic
            ? tensorNormal
            : localNormal;
        if (direction == Vector2.Zero)
        {
            direction = tensorNormal;
        }

        uint seed = unchecked((uint)UnpackInteger(
            plan.GetOption("Seed")));
        float random = Hash(
            x,
            y,
            seed + unchecked((uint)(pass.Iteration * 4099)));
        if (direction == Vector2.Zero)
        {
            float fallbackAngle = random * MathF.Tau;
            direction = new Vector2(
                MathF.Cos(fallbackAngle),
                MathF.Sin(fallbackAngle));
        }
        else if (!anisotropic)
        {
            float jitter =
                (random - 0.5f) *
                (1 - coherence) *
                (MathF.PI * 0.5f);
            direction = Rotate(direction, jitter);
        }

        Vector4 negative = Sample(
            source,
            width,
            height,
            x - (direction.X * stepX),
            y - (direction.Y * stepY));
        Vector4 positive = Sample(
            source,
            width,
            height,
            x + (direction.X * stepX),
            y + (direction.Y * stepY));
        float negativeLuminance = Luminance(negative);
        float positiveLuminance = Luminance(positive);
        float secondDerivative =
            negativeLuminance -
            (2 * centerLuminance) +
            positiveLuminance;

        Vector4 darker = center;
        float darkerLuminance = centerLuminance;
        SelectDarker(
            negative,
            negativeLuminance,
            ref darker,
            ref darkerLuminance);
        SelectDarker(
            positive,
            positiveLuminance,
            ref darker,
            ref darkerLuminance);

        Vector4 lighter = center;
        float lighterLuminance = centerLuminance;
        SelectLighter(
            negative,
            negativeLuminance,
            ref lighter,
            ref lighterLuminance);
        SelectLighter(
            positive,
            positiveLuminance,
            ref lighter,
            ref lighterLuminance);

        Vector4 target;
        if (mode == DarkenOnlyMode)
        {
            target = darker;
        }
        else if (mode == LightenOnlyMode)
        {
            target = lighter;
        }
        else if (secondDerivative > Epsilon)
        {
            target = darker;
        }
        else if (secondDerivative < -Epsilon)
        {
            target = lighter;
        }
        else
        {
            target = center;
        }

        float timeStep = anisotropic
            ? 0.45f * (0.25f + (0.75f * coherence))
            : 0.45f;
        Vector3 straight = Vector3.Lerp(
            Unpremultiply(center),
            Unpremultiply(target),
            timeStep);
        return new Vector4(
            Vector3.Clamp(straight, Vector3.Zero, Vector3.One) * center.W,
            center.W);
    }

    private static void AccumulateTensor(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        float stepX,
        float stepY,
        float weight,
        ref Vector3 tensor)
    {
        float gradientX = 0.5f * (
            Luminance(Sample(source, width, height, x + stepX, y)) -
            Luminance(Sample(source, width, height, x - stepX, y)));
        float gradientY = 0.5f * (
            Luminance(Sample(source, width, height, x, y + stepY)) -
            Luminance(Sample(source, width, height, x, y - stepY)));
        tensor.X += weight * gradientX * gradientX;
        tensor.Y += weight * gradientX * gradientY;
        tensor.Z += weight * gradientY * gradientY;
    }

    private static (Vector2 Direction, float Coherence)
        PrincipalDirection(Vector3 tensor)
    {
        float discriminant = MathF.Sqrt(MathF.Max(
            ((tensor.X - tensor.Z) * (tensor.X - tensor.Z)) +
            (4 * tensor.Y * tensor.Y),
            0));
        float largest = 0.5f * (
            tensor.X + tensor.Z + discriminant);
        float smallest = 0.5f * (
            tensor.X + tensor.Z - discriminant);
        Vector2 direction = MathF.Abs(tensor.Y) > Epsilon
            ? NormalizeOrZero(new Vector2(
                largest - tensor.Z,
                tensor.Y))
            : tensor.X >= tensor.Z
                ? Vector2.UnitX
                : Vector2.UnitY;
        if (largest <= Epsilon)
        {
            direction = Vector2.Zero;
        }
        float coherence = Math.Clamp(
            (largest - smallest) /
                MathF.Max(largest + smallest, Epsilon),
            0,
            1);
        return (direction, coherence);
    }

    private static Vector4 Sample(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        float clampedX = Math.Clamp(x, 0, width - 1);
        float clampedY = Math.Clamp(y, 0, height - 1);
        int left = (int)MathF.Floor(clampedX);
        int top = (int)MathF.Floor(clampedY);
        int right = Math.Min(left + 1, width - 1);
        int bottom = Math.Min(top + 1, height - 1);
        float horizontal = clampedX - left;
        float vertical = clampedY - top;
        Vector4 first = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            horizontal);
        Vector4 second = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            horizontal);
        return Vector4.Lerp(first, second, vertical);
    }

    private static Vector3 Unpremultiply(Vector4 color) =>
        color.W > Epsilon
            ? new Vector3(color.X, color.Y, color.Z) / color.W
            : Vector3.Zero;

    private static float Luminance(Vector4 color)
    {
        Vector3 straight = Unpremultiply(color);
        return
            (straight.X * 0.2126f) +
            (straight.Y * 0.7152f) +
            (straight.Z * 0.0722f);
    }

    private static Vector2 NormalizeOrZero(Vector2 value)
    {
        float lengthSquared = value.LengthSquared();
        return lengthSquared > Epsilon * Epsilon
            ? value / MathF.Sqrt(lengthSquared)
            : Vector2.Zero;
    }

    private static Vector2 Rotate(Vector2 value, float angle)
    {
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(
            (cosine * value.X) - (sine * value.Y),
            (sine * value.X) + (cosine * value.Y));
    }

    private static void SelectDarker(
        Vector4 candidate,
        float candidateLuminance,
        ref Vector4 selected,
        ref float selectedLuminance)
    {
        if (candidateLuminance < selectedLuminance)
        {
            selected = candidate;
            selectedLuminance = candidateLuminance;
        }
    }

    private static void SelectLighter(
        Vector4 candidate,
        float candidateLuminance,
        ref Vector4 selected,
        ref float selectedLuminance)
    {
        if (candidateLuminance > selectedLuminance)
        {
            selected = candidate;
            selectedLuminance = candidateLuminance;
        }
    }

    private static int UnpackInteger(Vector4 value)
    {
        uint low = (uint)Math.Clamp(MathF.Round(value.X), 0, 65535);
        uint high = (uint)Math.Clamp(MathF.Round(value.Y), 0, 65535);
        return unchecked((int)(low | (high << 16)));
    }

    private static float Hash(int x, int y, uint seed)
    {
        uint value =
            (unchecked((uint)x) * 0x9e3779b9u) ^
            (unchecked((uint)y) * 0x85ebca6bu) ^
            (seed * 0xc2b2ae35u);
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777216f;
    }
}
