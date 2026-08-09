using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogReliefMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogInkMath
{
    internal static Vector4[] Watercolor(
        PrismCatalogFilterPlan plan,
        Vector4[] original,
        int width,
        int height)
    {
        Vector4[] current = WatercolorMeanShift(
            plan,
            plan.Passes[0],
            original,
            width,
            height);
        current = WatercolorMeanShift(
            plan,
            plan.Passes[1],
            current,
            width,
            height);
        for (int passIndex = 2; passIndex <= 5; passIndex++)
        {
            current = WatercolorMorphology(
                plan.Passes[passIndex],
                current,
                width,
                height,
                erode: passIndex is 2 or 5);
        }

        Vector4[] result = new Vector4[current.Length];
        float edgeRadius = MathF.Max(
            plan.Passes[6].RadiusX,
            plan.Passes[6].RadiusY);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[(y * width) + x] = WatercolorComposite(
                    plan,
                    original,
                    current,
                    width,
                    height,
                    x,
                    y,
                    edgeRadius);
            }
        }
        return result;
    }

    private static Vector4[] WatercolorMeanShift(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] result = new Vector4[source.Length];
        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        float detail = Math.Clamp(
            Option(plan, "BrushDetail", 9) / 16,
            0,
            1);
        float rangeSigma = 0.3f - (0.24f * detail);
        float rangeDivisor = 2 * rangeSigma * rangeSigma;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 center = SamplePixel(
                    source,
                    width,
                    height,
                    x,
                    y);
                if (center.W <= 0.000001f)
                {
                    result[(y * width) + x] = Vector4.Zero;
                    continue;
                }

                Vector3 centerColor = Unpremultiply(center);
                Vector3 colorTotal = Vector3.Zero;
                float weightTotal = 0;
                for (int offsetY = -3; offsetY <= 3; offsetY++)
                {
                    for (int offsetX = -3; offsetX <= 3; offsetX++)
                    {
                        Vector4 sample = SamplePixelBilinear(
                            source,
                            width,
                            height,
                            x + (offsetX * radius / 3),
                            y + (offsetY * radius / 3));
                        if (sample.W <= 0.000001f)
                        {
                            continue;
                        }

                        Vector3 sampleColor = Unpremultiply(sample);
                        Vector3 difference = sampleColor - centerColor;
                        float spatialDistance =
                            (offsetX * offsetX) +
                            (offsetY * offsetY);
                        float weight = MathF.Exp(
                                -spatialDistance / 6f) *
                            MathF.Exp(
                                -Vector3.Dot(difference, difference) /
                                rangeDivisor) *
                            MathF.Exp(-MathF.Abs(sample.W - center.W) * 8);
                        colorTotal += sampleColor * weight;
                        weightTotal += weight;
                    }
                }

                Vector3 shifted = weightTotal <= 0.000001f
                    ? centerColor
                    : colorTotal / weightTotal;
                result[(y * width) + x] = Associated(
                    Vector3.Clamp(
                        shifted,
                        Vector3.Zero,
                        Vector3.One),
                    center.W);
            }
        }
        return result;
    }

    private static Vector4[] WatercolorMorphology(
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        bool erode)
    {
        Vector4[] result = new Vector4[source.Length];
        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 center = SamplePixel(
                    source,
                    width,
                    height,
                    x,
                    y);
                if (center.W <= 0.000001f)
                {
                    result[(y * width) + x] = Vector4.Zero;
                    continue;
                }

                Vector3 selected = erode
                    ? Vector3.One
                    : Vector3.Zero;
                bool found = false;
                for (int offsetY = -2; offsetY <= 2; offsetY++)
                {
                    for (int offsetX = -2; offsetX <= 2; offsetX++)
                    {
                        if ((offsetX * offsetX) +
                                (offsetY * offsetY) >
                            4)
                        {
                            continue;
                        }
                        Vector4 sample = SamplePixelBilinear(
                            source,
                            width,
                            height,
                            x + (offsetX * radius / 2),
                            y + (offsetY * radius / 2));
                        if (sample.W <= 0.000001f ||
                            MathF.Abs(sample.W - center.W) > 0.25f)
                        {
                            continue;
                        }

                        Vector3 color = Unpremultiply(sample);
                        selected = erode
                            ? Vector3.Min(selected, color)
                            : Vector3.Max(selected, color);
                        found = true;
                    }
                }

                result[(y * width) + x] = Associated(
                    found ? selected : Unpremultiply(center),
                    center.W);
            }
        }
        return result;
    }

    private static Vector4 WatercolorComposite(
        PrismCatalogFilterPlan plan,
        Vector4[] original,
        Vector4[] abstracted,
        int width,
        int height,
        int x,
        int y,
        float edgeRadius)
    {
        Vector4 originalPixel = SamplePixel(
            original,
            width,
            height,
            x,
            y);
        if (originalPixel.W <= 0.000001f)
        {
            return Vector4.Zero;
        }

        float textureStrength = Math.Clamp(
            Option(plan, "Texture", 3) / 10,
            0,
            1);
        float paper = WatercolorPaperHeight(x, y);
        float paperHorizontal =
            WatercolorPaperHeight(x + 1, y) -
            WatercolorPaperHeight(x - 1, y);
        float paperVertical =
            WatercolorPaperHeight(x, y + 1) -
            WatercolorPaperHeight(x, y - 1);
        float wobble = textureStrength * 0.45f;
        float sampleX = x + (paperHorizontal * wobble);
        float sampleY = y + (paperVertical * wobble);
        Vector4 baseSample = SamplePixelBilinear(
            abstracted,
            width,
            height,
            sampleX,
            sampleY);
        Vector3 color = Unpremultiply(baseSample);

        Vector3 horizontal = Vector3.Abs(
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX - edgeRadius,
                sampleY)) -
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX + edgeRadius,
                sampleY)));
        Vector3 vertical = Vector3.Abs(
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX,
                sampleY - edgeRadius)) -
            Unpremultiply(SamplePixelBilinear(
                abstracted,
                width,
                height,
                sampleX,
                sampleY + edgeRadius)));
        float edge = Math.Clamp(
            (horizontal.X + horizontal.Y + horizontal.Z +
                vertical.X + vertical.Y + vertical.Z) / 6,
            0,
            1);
        float shadow = Math.Clamp(
            Option(plan, "ShadowIntensity", 1),
            0,
            4);
        color = WatercolorPigmentDensity(
            color,
            1 + (edge * shadow * 0.85f));

        if (textureStrength > 0)
        {
            float turbulence = WatercolorValueNoise(
                x / 32f,
                y / 32f,
                0x9a4e21d3u);
            float dispersion =
                (0.65f * WatercolorValueNoise(
                    x / 4f,
                    y / 4f,
                    0x68bc21ebu)) +
                (0.35f * WatercolorValueNoise(
                    x / 1.75f,
                    y / 1.75f,
                    0x2e5be93du));
            color = WatercolorPigmentDensity(
                color,
                1 + ((turbulence - 0.5f) *
                    0.28f * textureStrength));
            color = WatercolorPigmentDensity(
                color,
                1 + ((dispersion - 0.5f) *
                    0.2f * textureStrength));
            color = WatercolorPigmentDensity(
                color,
                1 + ((paper - 0.5f) *
                    0.34f * textureStrength));

            float dryThreshold = 0.72f -
                (0.12f * textureStrength);
            float dryGap = SmoothStep(
                    dryThreshold,
                    dryThreshold + 0.14f,
                    paper) *
                textureStrength *
                (0.25f + (0.4f * StraightLuminance(color)));
            color = Vector3.Lerp(color, Vector3.One, dryGap);
        }

        return Associated(
            Vector3.Clamp(color, Vector3.Zero, Vector3.One),
            originalPixel.W);
    }

    private static Vector3 WatercolorPigmentDensity(
        Vector3 color,
        float density)
    {
        Vector3 result = color -
            ((color - (color * color)) * (density - 1));
        return Vector3.Clamp(result, Vector3.Zero, Vector3.One);
    }

    private static float WatercolorPaperHeight(
        float x,
        float y)
    {
        float fine = WatercolorValueNoise(
            x / 2.5f,
            y / 2.5f,
            0x51ed270bu);
        float coarse = WatercolorValueNoise(
            x / 13f,
            y / 13f,
            0x8321ca5du);
        float fiberX = 0.5f +
            (0.5f * MathF.Cos((x + (coarse * 2)) * 2.1f));
        float fiberY = 0.5f +
            (0.5f * MathF.Cos((y - (fine * 2)) * 2.35f));
        return Math.Clamp(
            (0.46f * fine) +
            (0.28f * coarse) +
            (0.13f * fiberX) +
            (0.13f * fiberY),
            0,
            1);
    }

    private static float WatercolorValueNoise(
        float x,
        float y,
        uint seed)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float horizontal = x - cellX;
        float vertical = y - cellY;
        horizontal = horizontal * horizontal *
            (3 - (2 * horizontal));
        vertical = vertical * vertical *
            (3 - (2 * vertical));
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

    internal static Vector4[] ChalkCharcoal(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float minimumWeight = 0.000001f;
        float charcoalArea = Math.Clamp(
            Option(plan, "CharcoalArea", 2),
            0,
            20);
        float chalkArea = Math.Clamp(
            Option(plan, "ChalkArea", 1),
            0,
            20);
        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 4) / 10,
            0,
            1);
        Vector4 foregroundOption = plan.GetOption("Foreground");
        Vector4 backgroundOption = plan.GetOption("Background");
        Vector3 foreground = Vector3.Clamp(
            new Vector3(foregroundOption.X, foregroundOption.Y, foregroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        Vector3 background = Vector3.Clamp(
            new Vector3(backgroundOption.X, backgroundOption.Y, backgroundOption.Z),
            Vector3.Zero,
            Vector3.One);
        float sigma = Math.Clamp(plan.Options5.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options5.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options5.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options5.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        float sharpen = float.Lerp(4, 16, pressure);
        float epsilon = float.Lerp(0.035f, -0.015f, pressure);
        float phi = float.Lerp(16, 42, pressure);
        float darkThreshold = float.Lerp(0.24f, 0.72f, charcoalArea / 20);
        float lightThreshold = float.Lerp(0.84f, 0.42f, chalkArea / 20);
        float grainStrength = 0.12f + (0.2f * pressure);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(phi * (response - epsilon)),
                    0,
                    1);
            float edgeMask = 1 - thresholded;
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            float luminance = StraightLuminance(straight);
            float darkTone = 1 - SmoothStep(
                darkThreshold - 0.18f,
                darkThreshold + 0.12f,
                luminance);
            float lightTone = SmoothStep(
                lightThreshold - 0.12f,
                lightThreshold + 0.18f,
                luminance);
            int x = index % width;
            int y = index / width;
            float grain = ChalkCharcoalValueNoise(x, y);
            float fiber = 0.5f +
                (0.5f * MathF.Sin(
                    (x * 1.73f) +
                    (y * 0.19f) +
                    (grain * 3.1f)));
            float centeredGrain = (grain - 0.5f) * 2;
            float charcoalGrain = Math.Clamp(
                0.78f +
                    (centeredGrain * grainStrength) -
                    ((fiber - 0.5f) * 0.18f),
                0,
                1);
            float chalkGrain = Math.Clamp(
                0.82f -
                    (centeredGrain * grainStrength * 0.8f) +
                    ((fiber - 0.5f) * 0.14f),
                0,
                1);
            float darkMask = Math.Clamp(
                MathF.Max(edgeMask, darkTone * 0.82f) *
                    charcoalGrain *
                    (0.72f + (0.28f * pressure)),
                0,
                1);
            float lightMask = Math.Clamp(
                lightTone * chalkGrain *
                    (0.68f + (0.32f * pressure)) *
                    (1 - (darkMask * 0.8f)),
                0,
                1);
            Vector3 toned = Vector3.Lerp(
                foreground,
                background,
                SmoothStep(0.2f, 0.8f, luminance));
            toned = Vector3.Lerp(toned, foreground, darkMask);
            toned = Vector3.Lerp(toned, background, lightMask);
            result[index] = Associated(
                Vector3.Clamp(toned, Vector3.Zero, Vector3.One),
                alpha);
        }

        return result;
    }

    private static float ChalkCharcoalValueNoise(int x, int y)
    {
        float fine = ChalkCharcoalHash(x + 17, y + 43);
        float coarse = ChalkCharcoalHash(
            MathF.Floor(x * 0.25f) + 71,
            MathF.Floor(y * 0.25f) + 29);
        return Math.Clamp((0.72f * fine) + (0.28f * coarse), 0, 1);
    }

    private static float ChalkCharcoalHash(float x, float y)
    {
        float valueX = Fraction(x * 0.1031f);
        float valueY = Fraction(y * 0.1031f);
        float valueZ = valueX;
        float shift =
            (valueX * (valueY + 33.33f)) +
            (valueY * (valueZ + 33.33f)) +
            (valueZ * (valueX + 33.33f));
        valueX += shift;
        valueY += shift;
        valueZ += shift;
        return Fraction((valueX + valueY) * valueZ);
    }

    internal static Vector4[] AccentedEdges(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float epsilon = 0.02f;
        const float sharpen = 10;
        const float minimumWeight = 0.000001f;
        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options3.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        float edgeTone = Math.Clamp(
            Option(plan, "EdgeBrightness", 1) / 50,
            0,
            1);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 2) / 15,
            0,
            1);
        float phi = float.Lerp(48, 6, smoothness);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(phi * (response - epsilon)),
                    0,
                    1);
            float accent = 1 - thresholded;
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            result[index] = Associated(
                Vector3.Lerp(straight, new Vector3(edgeTone), accent),
                alpha);
        }

        return result;
    }

    internal static Vector4[] DarkStrokes(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float sharpen = 10;
        const float minimumWeight = 0.000001f;
        float balance = Math.Clamp(Option(plan, "Balance", 0) / 10, 0, 1);
        float blackIntensity = Math.Clamp(
            Option(plan, "BlackIntensity", 1) / 10,
            0,
            1);
        float whiteIntensity = Math.Clamp(
            Option(plan, "WhiteIntensity", 2) / 10,
            0,
            1);
        if (blackIntensity <= 0 && whiteIntensity <= 0)
        {
            return (Vector4[])source.Clone();
        }

        float epsilon = float.Lerp(-0.04f, 0.06f, balance);
        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options3.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(24 * (response - epsilon)),
                    0,
                    1);
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            float luminance = StraightLuminance(straight);
            float darkMask = Math.Clamp(
                (1 - thresholded) + ((1 - luminance) * 0.35f),
                0,
                1);
            float lightMask = thresholded * luminance;
            Vector3 toned = Vector3.Lerp(
                straight,
                Vector3.Zero,
                blackIntensity * darkMask);
            toned = Vector3.Lerp(
                toned,
                Vector3.One,
                whiteIntensity * lightMask);
            result[index] = Associated(toned, alpha);
        }

        return result;
    }

    internal static Vector4[] InkOutlines(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float epsilon = 0.02f;
        const float minimumWeight = 0.000001f;
        float darkIntensity = Math.Clamp(
            Option(plan, "DarkIntensity", 20),
            0,
            50);
        float lightIntensity = Math.Clamp(
            Option(plan, "LightIntensity", 10),
            0,
            50);
        if (darkIntensity <= 0 && lightIntensity <= 0)
        {
            return (Vector4[])source.Clone();
        }

        float sharpen = Math.Clamp(darkIntensity, 0, 64);
        float darkStrength = darkIntensity / 50;
        float lightStrength = lightIntensity / 50;
        float phi = Math.Clamp(8 + (80 * lightStrength), 8, 48);
        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(plan.Options3.Y, sigma, 6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            source,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);
        Vector4[] result = new Vector4[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            if (alpha <= minimumWeight)
            {
                result[index] = Vector4.Zero;
                continue;
            }

            float narrowLuminance = narrow[index].Y <= minimumWeight
                ? 0
                : narrow[index].X / narrow[index].Y;
            float extendedLuminance = extended[index].Y <= minimumWeight
                ? 0
                : extended[index].X / extended[index].Y;
            float response =
                ((sharpen + 1) * narrowLuminance) -
                (sharpen * extendedLuminance);
            float thresholded = response >= epsilon
                ? 1
                : Math.Clamp(
                    1 + MathF.Tanh(phi * (response - epsilon)),
                    0,
                    1);
            Vector3 straight = Vector3.Clamp(
                Unpremultiply(source[index]),
                Vector3.Zero,
                Vector3.One);
            float inkMask = 1 - thresholded;
            float paperMask = thresholded * StraightLuminance(straight);
            Vector3 outlined = Vector3.Lerp(
                straight,
                Vector3.Zero,
                darkStrength * inkMask);
            outlined = Vector3.Lerp(
                outlined,
                Vector3.One,
                lightStrength * paperMask);
            result[index] = Associated(outlined, alpha);
        }

        return result;
    }

    internal static Vector4[] SumiE(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        int washRadius = Math.Clamp(
            (int)MathF.Ceiling(plan.Passes[0].RadiusX),
            1,
            6);
        Vector4[] wash = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                wash[(y * width) + x] = SumiEDirectionalWash(
                    plan,
                    source,
                    width,
                    height,
                    x,
                    y,
                    washRadius);
            }
        }

        float sigma = Math.Clamp(plan.Options3.X, 0.5f, 4);
        float extendedSigma = Math.Clamp(
            plan.Options3.Y,
            sigma,
            6.4f);
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Options3.Z),
            1,
            8);
        int extendedRadius = Math.Clamp(
            (int)MathF.Round(plan.Options3.W),
            radius,
            8);
        (Vector2[] narrow, Vector2[] extended) = XDogLuminance(
            wash,
            width,
            height,
            sigma,
            radius,
            extendedSigma,
            extendedRadius);

        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 2) / 8,
            0,
            1);
        float contrast = Math.Clamp(
            Option(plan, "Contrast", 2),
            -3,
            10);
        float sharpen = 4 + (24 * pressure);
        float epsilon = 0.015f + (0.004f * contrast);
        float phi = Math.Clamp(12 + (8 * contrast), 6, 64);
        float tonalContrast = Math.Clamp(
            1 + (0.22f * contrast),
            0.25f,
            3.2f);
        Vector3 inkColor = new(0.012f, 0.018f, 0.017f);
        Vector3 paperColor = new(0.985f, 0.978f, 0.948f);
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float alpha = Math.Clamp(source[index].W, 0, 1);
                if (alpha <= 0.000001f)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float narrowLuminance = narrow[index].Y <= 0.000001f
                    ? 0
                    : narrow[index].X / narrow[index].Y;
                float extendedLuminance =
                    extended[index].Y <= 0.000001f
                        ? 0
                        : extended[index].X / extended[index].Y;
                float response =
                    ((sharpen + 1) * narrowLuminance) -
                    (sharpen * extendedLuminance);
                float thresholded = response >= epsilon
                    ? 1
                    : Math.Clamp(
                        1 + MathF.Tanh(phi * (response - epsilon)),
                        0,
                        1);
                float inkEdge = 1 - thresholded;
                float tone = Math.Clamp(
                    ((narrowLuminance - 0.5f) * tonalContrast) + 0.5f,
                    0,
                    1);
                float quantizedTone = MathF.Floor((tone * 4) + 0.5f) / 4;
                tone = float.Lerp(tone, quantizedTone, 0.68f);
                tone = Math.Clamp(
                    tone -
                    (inkEdge * float.Lerp(0.42f, 0.9f, pressure)) -
                    ((1 - narrowLuminance) * pressure * 0.08f),
                    0,
                    1);

                float fineGrain = Hash(x, y, 0x51ed270bu);
                float coarseGrain = Hash(x / 9, y / 9, 0x8321ca5du);
                float fiber = 0.5f +
                    (0.25f * MathF.Cos(
                        (x + (coarseGrain * 2)) * 1.7f)) +
                    (0.25f * MathF.Cos(
                        (y - (fineGrain * 2)) * 2.15f));
                float paper = Math.Clamp(
                    (0.45f * fineGrain) +
                    (0.25f * coarseGrain) +
                    (0.30f * fiber),
                    0,
                    1);
                float dryGap = SmoothStep(0.70f, 0.94f, paper) *
                    (1 - (pressure * 0.72f)) *
                    (1 - tone) *
                    0.42f;
                tone = Math.Clamp(
                    float.Lerp(tone, 1, dryGap) +
                    ((paper - 0.5f) * 0.055f),
                    0,
                    1);
                result[index] = Associated(
                    Vector3.Lerp(inkColor, paperColor, tone),
                    alpha);
            }
        }

        return result;
    }

    private static Vector4 SumiEDirectionalWash(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        int radius)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0.000001f)
        {
            return Vector4.Zero;
        }

        float horizontal =
            Luminance(SamplePixel(source, width, height, x + 1, y)) -
            Luminance(SamplePixel(source, width, height, x - 1, y));
        float vertical =
            Luminance(SamplePixel(source, width, height, x, y + 1)) -
            Luminance(SamplePixel(source, width, height, x, y - 1));
        Vector2 gradient = new(horizontal, vertical);
        Vector2 tangent = gradient.LengthSquared() <= 0.000001f
            ? Vector2.UnitX
            : Vector2.Normalize(new Vector2(-gradient.Y, gradient.X));
        Vector2 normal = new(-tangent.Y, tangent.X);
        Span<float> sums = stackalloc float[4];
        Span<float> squaredSums = stackalloc float[4];
        Span<float> weights = stackalloc float[4];
        float radiusSquared = radius * radius;
        for (int offsetY = -radius; offsetY <= radius; offsetY++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                float distanceSquared =
                    (offsetX * offsetX) + (offsetY * offsetY);
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                Vector4 sample = SamplePixel(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY);
                float alphaConfidence = Math.Clamp(
                    1 - (MathF.Abs(sample.W - center.W) * 4),
                    0,
                    1);
                float weight = sample.W * alphaConfidence * MathF.Exp(
                    -2 * distanceSquared / MathF.Max(radiusSquared, 1));
                if (weight <= 0)
                {
                    continue;
                }

                float luminance = Luminance(sample);
                Vector2 offset = new(offsetX, offsetY);
                float tangentPosition = Vector2.Dot(offset, tangent);
                float normalPosition = Vector2.Dot(offset, normal);
                if (tangentPosition <= 0 && normalPosition <= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        0,
                        luminance,
                        weight);
                }
                if (tangentPosition >= 0 && normalPosition <= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        1,
                        luminance,
                        weight);
                }
                if (tangentPosition >= 0 && normalPosition >= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        2,
                        luminance,
                        weight);
                }
                if (tangentPosition <= 0 && normalPosition >= 0)
                {
                    AccumulateSumiESector(
                        sums,
                        squaredSums,
                        weights,
                        3,
                        luminance,
                        weight);
                }
            }
        }

        float pressure = Math.Clamp(
            Option(plan, "StrokePressure", 2) / 8,
            0,
            1);
        float sharpness = float.Lerp(2, 7, pressure);
        float weightedMean = 0;
        float confidenceTotal = 0;
        for (int sector = 0; sector < 4; sector++)
        {
            float sectorWeight = MathF.Max(weights[sector], 0.000001f);
            float mean = sums[sector] / sectorWeight;
            float variance = MathF.Max(
                MathF.Abs(
                    (squaredSums[sector] / sectorWeight) -
                    (mean * mean)),
                0.000001f);
            float confidence = 1 /
                (1 + MathF.Pow(400 * variance, sharpness));
            weightedMean += mean * confidence;
            confidenceTotal += confidence;
        }

        float centerLuminance = Luminance(center);
        float wash = confidenceTotal <= 0.000001f
            ? centerLuminance
            : weightedMean / confidenceTotal;
        wash = float.Lerp(centerLuminance, wash, 0.88f);
        return Associated(new Vector3(Math.Clamp(wash, 0, 1)), center.W);
    }

    private static void AccumulateSumiESector(
        Span<float> sums,
        Span<float> squaredSums,
        Span<float> weights,
        int sector,
        float luminance,
        float weight)
    {
        sums[sector] += luminance * weight;
        squaredSums[sector] += luminance * luminance * weight;
        weights[sector] += weight;
    }

    private static (Vector2[] Narrow, Vector2[] Extended) XDogLuminance(
        Vector4[] source,
        int width,
        int height,
        float sigma,
        int radius,
        float extendedSigma,
        int extendedRadius)
    {
        const float minimumWeight = 0.000001f;
        Vector2[] weightedLuminance = new Vector2[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            float alpha = Math.Clamp(source[index].W, 0, 1);
            float luminance = alpha <= minimumWeight
                ? 0
                : StraightLuminance(Unpremultiply(source[index]));
            weightedLuminance[index] = new Vector2(
                luminance * alpha,
                alpha);
        }

        return (
            XDogGaussianBlur(
                weightedLuminance,
                width,
                height,
                sigma,
                radius),
            XDogGaussianBlur(
                weightedLuminance,
                width,
                height,
                extendedSigma,
                extendedRadius));
    }

    private static Vector2[] XDogGaussianBlur(
        Vector2[] source,
        int width,
        int height,
        float sigma,
        int radius)
    {
        Vector2[] horizontal = new Vector2[source.Length];
        Vector2[] output = new Vector2[source.Length];
        float inverseSigma = 1 / (2 * sigma * sigma);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 total = Vector2.Zero;
                float totalWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    float weight = MathF.Exp(
                        -(offset * offset) * inverseSigma);
                    int sampleX = Math.Clamp(x + offset, 0, width - 1);
                    total += source[(y * width) + sampleX] * weight;
                    totalWeight += weight;
                }

                horizontal[(y * width) + x] = total / totalWeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 total = Vector2.Zero;
                float totalWeight = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    float weight = MathF.Exp(
                        -(offset * offset) * inverseSigma);
                    int sampleY = Math.Clamp(y + offset, 0, height - 1);
                    total += horizontal[(sampleY * width) + x] * weight;
                    totalWeight += weight;
                }

                output[(y * width) + x] = total / totalWeight;
            }
        }

        return output;
    }

    internal static Vector4[] PosterEdges(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        const float minimumWeight = 0.000001f;
        int radius = Math.Clamp(
            (int)MathF.Round(plan.Passes[0].RadiusX),
            1,
            8);
        float edgeIntensity = Math.Clamp(
            Option(plan, "EdgeIntensity", 1),
            0,
            4);
        int levels = Math.Clamp(
            (int)MathF.Round(Option(plan, "Posterization", 2)),
            2,
            32);
        (float[] alpha, float[] guidedLuminance,
            Vector3[] guidedColor) = GuidedFilter(
                source,
                width,
                height,
                radius,
                epsilon: 0.01f);
        int pixelCount = checked(width * height);

        Vector4[] result = new Vector4[pixelCount];
        float levelScale = levels - 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float pixelAlpha = alpha[index];
                if (pixelAlpha <= minimumWeight)
                {
                    result[index] = Vector4.Zero;
                    continue;
                }

                float edge = Math.Clamp(
                    GuidedScharrGradient(
                        guidedLuminance,
                        width,
                        height,
                        x,
                        y,
                        radius).Length(),
                    0,
                    1);
                float ink = Math.Clamp(
                    edge * edgeIntensity * 2,
                    0,
                    1);
                Vector3 quantized = new(
                    MathF.Round(guidedColor[index].X * levelScale) /
                        levelScale,
                    MathF.Round(guidedColor[index].Y * levelScale) /
                        levelScale,
                    MathF.Round(guidedColor[index].Z * levelScale) /
                        levelScale);
                result[index] = Associated(
                    Vector3.Clamp(
                        quantized * (1 - ink),
                        Vector3.Zero,
                        Vector3.One),
                    pixelAlpha);
            }
        }

        return result;
    }
}
