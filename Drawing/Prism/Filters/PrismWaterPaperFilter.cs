using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismWaterPaperFilter
{
    private const float MinimumAlpha = 0.000001f;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] pigment = PreparePigment(
            plan,
            source,
            width,
            height);
        return ApplySubstrate(
            plan,
            source,
            pigment,
            width,
            height);
    }

    private static Vector4[] PreparePigment(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float fiberLength = Math.Clamp(
            plan.GetOption("FiberLength").X,
            1,
            96);
        float radius = Math.Clamp(
            MathF.Sqrt(fiberLength) * 0.75f,
            1,
            6);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Vector4 center = source[index];
                if (center.W <= MinimumAlpha)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                Vector3 centerColor = Unpremultiply(center);
                Vector3 colorTotal = Vector3.Zero;
                float weightTotal = 0;
                for (int offsetY = -2; offsetY <= 2; offsetY++)
                {
                    for (int offsetX = -2; offsetX <= 2; offsetX++)
                    {
                        Vector4 sample = SampleBilinear(
                            source,
                            width,
                            height,
                            x + (offsetX * radius / 2),
                            y + (offsetY * radius / 2));
                        if (sample.W <= MinimumAlpha)
                        {
                            continue;
                        }

                        Vector3 sampleColor = Unpremultiply(sample);
                        Vector3 difference = sampleColor - centerColor;
                        float spatialDistance =
                            (offsetX * offsetX) +
                            (offsetY * offsetY);
                        float weight = MathF.Exp(-spatialDistance / 5.5f) *
                            MathF.Exp(-Vector3.Dot(difference, difference) * 4) *
                            MathF.Exp(-MathF.Abs(sample.W - center.W) * 8);
                        colorTotal += sampleColor * weight;
                        weightTotal += weight;
                    }
                }

                Vector3 bled = weightTotal <= MinimumAlpha
                    ? centerColor
                    : colorTotal / weightTotal;
                Vector3 prepared = Vector3.Lerp(
                    centerColor,
                    bled,
                    0.72f);
                result[index] = Associated(
                    Vector3.Clamp(prepared, Vector3.Zero, Vector3.One),
                    center.W);
            }
        }
        return result;
    }

    private static Vector4[] ApplySubstrate(
        PrismCatalogFilterPlan plan,
        Vector4[] original,
        Vector4[] pigment,
        int width,
        int height)
    {
        float fiberLength = Math.Clamp(
            plan.GetOption("FiberLength").X,
            1,
            96);
        float brightness = Math.Clamp(
            plan.GetOption("Brightness").X,
            0,
            100);
        float contrast = Math.Clamp(
            plan.GetOption("Contrast").X,
            0,
            100) / 100;
        uint seed = UnpackInteger(plan.GetOption("Seed"));
        float brightnessOffset = (brightness - 50) / 100;
        float contrastGain = float.Lerp(0.65f, 1.8f, contrast);
        float warpStrength = Math.Clamp(
            0.35f + (fiberLength * 0.02f),
            0.35f,
            1.5f);
        float edgeStep = Math.Clamp(
            MathF.Sqrt(fiberLength) * 0.25f,
            1,
            3);
        Vector4[] result = new Vector4[original.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = original[index].W;
                if (alpha <= MinimumAlpha)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float paper = SubstrateHeight(
                    x,
                    y,
                    fiberLength,
                    seed);
                float horizontal =
                    SubstrateHeight(x + 1, y, fiberLength, seed) -
                    SubstrateHeight(x - 1, y, fiberLength, seed);
                float vertical =
                    SubstrateHeight(x, y + 1, fiberLength, seed) -
                    SubstrateHeight(x, y - 1, fiberLength, seed);
                float sampleX = x + (horizontal * warpStrength);
                float sampleY = y + (vertical * warpStrength);
                Vector3 color = Unpremultiply(
                    SampleBilinear(
                        pigment,
                        width,
                        height,
                        sampleX,
                        sampleY));

                Vector3 edgeDelta = Vector3.Abs(
                        Unpremultiply(SampleBilinear(
                            pigment,
                            width,
                            height,
                            sampleX - edgeStep,
                            sampleY)) -
                        Unpremultiply(SampleBilinear(
                            pigment,
                            width,
                            height,
                            sampleX + edgeStep,
                            sampleY))) +
                    Vector3.Abs(
                        Unpremultiply(SampleBilinear(
                            pigment,
                            width,
                            height,
                            sampleX,
                            sampleY - edgeStep)) -
                        Unpremultiply(SampleBilinear(
                            pigment,
                            width,
                            height,
                            sampleX,
                            sampleY + edgeStep)));
                float edge = Math.Clamp(
                    (edgeDelta.X + edgeDelta.Y + edgeDelta.Z) / 6,
                    0,
                    1);
                float density = 1 +
                    ((0.5f - paper) * (0.45f + (contrast * 0.35f))) +
                    (edge * (0.35f + (contrast * 0.65f)));
                color = ApplyPigmentDensity(color, density);

                float dryGap = SmoothStep(0.68f, 0.9f, paper) *
                    (0.035f + (contrast * 0.12f)) *
                    (0.35f + (Luminance(color) * 0.65f));
                color = Vector3.Lerp(color, Vector3.One, dryGap);
                color = ((color - new Vector3(0.5f)) * contrastGain) +
                    new Vector3(0.5f + brightnessOffset);
                result[index] = Associated(
                    Vector3.Clamp(color, Vector3.Zero, Vector3.One),
                    alpha);
            }
        }
        return result;
    }

    private static float SubstrateHeight(
        float x,
        float y,
        float fiberLength,
        uint seed)
    {
        float angle = Hash(0, 0, seed + 0x51ed270bu) * MathF.PI;
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float along = (cosine * x) + (sine * y);
        float across = (-sine * x) + (cosine * y);
        float width = MathF.Max(1, fiberLength * 0.16f);
        float warp = ValueNoise(
                along / (fiberLength * 1.8f),
                across / MathF.Max(width * 2.5f, 1),
                seed + 0x8321ca5du) -
            0.5f;
        float primary = ValueNoise(
            (along + (warp * fiberLength * 0.75f)) / fiberLength,
            across / width,
            seed + 0x68bc21ebu);
        float secondary = ValueNoise(
            ((along * 0.55f) - (across * 0.18f)) /
                MathF.Max(fiberLength * 0.55f, 1),
            ((across * 0.45f) + (along * 0.04f)) /
                MathF.Max(width * 0.7f, 1),
            seed + 0x2e5be93du);
        float fine = ValueNoise(
            x / 2.2f,
            y / 2.2f,
            seed + 0x9a4e21d3u);
        float striation = 0.5f +
            (0.5f * MathF.Cos(
                ((across / width) + warp) *
                    (2 * MathF.PI) +
                (primary * 1.5f)));
        return Math.Clamp(
            (0.42f * primary) +
            (0.24f * secondary) +
            (0.2f * striation) +
            (0.14f * fine),
            0,
            1);
    }

    private static float ValueNoise(
        float x,
        float y,
        uint seed)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float horizontal = SmoothCurve(x - cellX);
        float vertical = SmoothCurve(y - cellY);
        float top = float.Lerp(
            Hash(cellX, cellY, seed),
            Hash(cellX + 1, cellY, seed),
            horizontal);
        float bottom = float.Lerp(
            Hash(cellX, cellY + 1, seed),
            Hash(cellX + 1, cellY + 1, seed),
            horizontal);
        return float.Lerp(top, bottom, vertical);
    }

    private static float Hash(int x, int y, uint seed)
    {
        uint value = unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            (seed * 0xc2b2ae35u);
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777215f;
    }

    private static Vector4 SampleBilinear(
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
        Vector4 upper = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            horizontal);
        Vector4 lower = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            horizontal);
        return Vector4.Lerp(upper, lower, vertical);
    }

    private static Vector3 ApplyPigmentDensity(
        Vector3 color,
        float density) =>
        Vector3.Clamp(
            color - ((color - (color * color)) * (density - 1)),
            Vector3.Zero,
            Vector3.One);

    private static uint UnpackInteger(Vector4 value) =>
        ((uint)value.Y << 16) |
        ((uint)value.X & 0xffffu);

    private static Vector3 Unpremultiply(Vector4 color) =>
        color.W <= MinimumAlpha
            ? Vector3.Zero
            : new Vector3(color.X, color.Y, color.Z) / color.W;

    private static Vector4 Associated(Vector3 color, float alpha) =>
        new(color * alpha, alpha);

    private static float Luminance(Vector3 color) =>
        Vector3.Dot(color, new Vector3(0.2126f, 0.7152f, 0.0722f));

    private static float SmoothCurve(float value) =>
        value * value * (3 - (2 * value));

    private static float SmoothStep(float edge0, float edge1, float value) =>
        SmoothCurve(Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1));
}
