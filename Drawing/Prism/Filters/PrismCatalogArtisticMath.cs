using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogColorMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogArtisticMath
{
    internal static Vector4 Artistic(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        if (plan.Filter == PrismFilterId.DryBrush)
        {
            return PrismDryBrushFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Cutout)
        {
            return PrismCutoutFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.FilmGrain)
        {
            return PrismFilmGrainFilter.ApplyPixel(
                plan,
                SamplePixel(source, width, height, x, y),
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.AngledStrokes)
        {
            return PrismAngledStrokesFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.PaintDaubs)
        {
            return PrismPaintDaubsFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.PaletteKnife)
        {
            return PrismPaletteKnifeFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.PlasticWrap)
        {
            return PrismPlasticWrapFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.RoughPastels)
        {
            return PrismRoughPastelsFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.SmudgeStick)
        {
            return PrismSmudgeStickFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Sponge)
        {
            return PrismSpongeFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Underpainting)
        {
            return PrismUnderpaintingFilter.ApplyPixel(
                plan,
                pass,
                source,
                width,
                height,
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Crosshatch)
        {
            return PrismCrosshatchFilter.ApplyPixel(
                plan,
                SamplePixel(source, width, height, x, y),
                x,
                y);
        }
        if (plan.Filter == PrismFilterId.Spatter)
        {
            return PrismSpatterFilter.ApplyPixel(
                plan, source, width, height, x, y);
        }
        if (plan.Filter == PrismFilterId.SprayedStrokes)
        {
            return PrismSprayedStrokesFilter.ApplyPixel(
                plan,
                source,
                width,
                height,
                x,
                y);
        }

        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        float edge = Sobel(source, width, height, x, y);
        int variant = ((int)plan.Filter - 77) % 6;
        float amount = Math.Clamp(
            ParameterMagnitude(plan) * 0.01f,
            0.05f,
            0.85f);
        float noise = Hash(x, y, Seed(plan, "Seed")) - 0.5f;
        Vector3 styled = variant switch
        {
            0 => Quantize(straight, 6) -
                new Vector3(edge * amount),
            1 => Vector3.Lerp(
                straight,
                new Vector3(Luminance(center)),
                amount),
            2 => Quantize(
                straight + new Vector3(noise * amount),
                8),
            3 => Vector3.Lerp(
                straight,
                Vector3.One - new Vector3(edge),
                amount),
            4 => Vector3.Lerp(
                straight,
                new Vector3(
                    straight.X * 1.1f,
                    straight.Y * 0.95f,
                    straight.Z * 0.8f),
                amount),
            _ => straight +
                new Vector3(
                    edge * amount,
                    -edge * amount * 0.5f,
                    noise * amount)
        };
        return Associated(
            Vector3.Clamp(styled, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 Spatter(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float sprayRadius = MathF.Max(
            0,
            Option(plan, "SprayRadius", 10));
        if (center.W <= 0 || sprayRadius <= 0)
        {
            return center;
        }

        PrismSpatterPointField field =
            PrismRecursiveWangBlueNoise.PointField;
        float averageSpacingInCells =
            field.GridSize / MathF.Sqrt(field.PointCount);
        float cellSize = sprayRadius / averageSpacingInCells;
        Vector2 seedOffset = PrismRecursiveWangBlueNoise.SeedOffset(
            plan.SpatterSeed);
        Vector2 pattern = new(
            ((x + 0.5f) / cellSize) + seedOffset.X,
            ((y + 0.5f) / cellSize) + seedOffset.Y);
        int baseCellX = (int)MathF.Floor(pattern.X);
        int baseCellY = (int)MathF.Floor(pattern.Y);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 5) / 15,
            0,
            1);
        float edgeWidth = 0.04f + (0.24f * smoothness);
        float bestCoverage = 0;
        Vector3 bestColor = Vector3.One;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int cellX = baseCellX + offsetX;
                int cellY = baseCellY + offsetY;
                for (int layer = 0;
                    layer < field.LayerCount;
                    layer++)
                {
                    Vector4 point = field.GetPoint(
                        cellX,
                        cellY,
                        layer);
                    if (point.W <= 0)
                    {
                        continue;
                    }

                    Vector2 pointPosition = new(
                        cellX + point.X,
                        cellY + point.Y);
                    Vector2 pointPixel =
                        (pointPosition - seedOffset) * cellSize;
                    Vector4 pointSource = SamplePixel(
                        source,
                        width,
                        height,
                        (int)MathF.Floor(pointPixel.X),
                        (int)MathF.Floor(pointPixel.Y));
                    float density =
                        (1 - Luminance(pointSource)) * pointSource.W;
                    if (point.Z > density)
                    {
                        continue;
                    }

                    float variationValue =
                        (point.X * 37) +
                        (point.Y * 91) +
                        (point.Z * 65_521);
                    float variation =
                        variationValue - MathF.Floor(variationValue);
                    float pointRadius = 0.68f + (0.2f * variation);
                    float distance = Vector2.Distance(
                        pattern,
                        pointPosition);
                    float coverage = 1 - SmoothStep(
                        pointRadius - edgeWidth,
                        pointRadius + edgeWidth,
                        distance);
                    if (coverage <= bestCoverage)
                    {
                        continue;
                    }

                    bestCoverage = coverage;
                    bestColor = Vector3.Clamp(
                        Unpremultiply(pointSource),
                        Vector3.Zero,
                        Vector3.One);
                }
            }
        }

        return Associated(
            Vector3.Lerp(Vector3.One, bestColor, bestCoverage),
            center.W);
    }

    internal static Vector4 SprayedStrokes(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float strokeLength = MathF.Max(
            Option(plan, "StrokeLength", 12),
            0);
        float sprayRadius = MathF.Max(
            Option(plan, "SprayRadius", 7),
            0);
        if (center.W <= 0 ||
            (strokeLength <= 0 && sprayRadius <= 0))
        {
            return center;
        }

        Vector2 direction = (int)MathF.Round(
            Option(plan, "Direction", 0)) switch
        {
            1 => Vector2.UnitX,
            2 => Vector2.Normalize(new Vector2(-1, 1)),
            3 => Vector2.UnitY,
            _ => Vector2.Normalize(Vector2.One)
        };
        Vector2 normal = new(-direction.Y, direction.X);
        uint seed = Seed(plan, "Seed");
        Vector2 seedOffset = PrismRecursiveWangBlueNoise.SeedOffset(seed);
        Vector2 pixel = new(x + 0.5f, y + 0.5f);
        float cellSize = MathF.Max(sprayRadius, 2);
        Vector2 pattern = (pixel / cellSize) + seedOffset;
        int baseCellX = (int)MathF.Floor(pattern.X);
        int baseCellY = (int)MathF.Floor(pattern.Y);
        Vector3 centerColor = Vector3.Clamp(
            Unpremultiply(center),
            Vector3.Zero,
            Vector3.One);
        Vector3 accumulated = Vector3.Zero;
        float totalWeight = 0;
        PrismSpatterPointField field =
            PrismRecursiveWangBlueNoise.PointField;

        for (int index = 0; index < 7; index++)
        {
            int cellOffsetX = index switch
            {
                0 or 5 => -1,
                2 or 6 => 1,
                _ => 0
            };
            int cellOffsetY = index switch
            {
                1 or 6 => -1,
                4 or 5 => 1,
                _ => 0
            };
            int cellX = baseCellX + cellOffsetX;
            int cellY = baseCellY + cellOffsetY;
            Vector4 point = field.GetPoint(cellX, cellY, layer: 0);
            uint salt = unchecked((uint)(index + 1) * 0x9e3779b9u);
            Vector2 jitter = point.W > 0
                ? new Vector2(point.X, point.Y)
                : new Vector2(
                    Hash(cellX, cellY, seed ^ salt),
                    Hash(cellX, cellY, seed ^ salt ^ 0x85ebca6bu));
            float position = (index - 3) / 6f;
            float longitudinal =
                (position * strokeLength) +
                ((jitter.X - 0.5f) * strokeLength / 7);
            float lateral =
                (jitter.Y - 0.5f) * 2 * sprayRadius;
            Vector2 samplePosition =
                pixel +
                (direction * longitudinal) +
                (normal * lateral);
            Vector4 sample = SamplePixelBilinear(
                source,
                width,
                height,
                samplePosition.X - 0.5f,
                samplePosition.Y - 0.5f);
            if (sample.W <= 0)
            {
                continue;
            }

            float weight = index switch
            {
                0 or 6 => 0.08f,
                1 or 5 => 0.12f,
                2 or 4 => 0.18f,
                _ => 0.24f
            };
            Vector3 sampleColor = Vector3.Clamp(
                Unpremultiply(sample),
                Vector3.Zero,
                Vector3.One);
            Vector3 colorDelta = sampleColor - centerColor;
            weight /=
                1 + (3 * Vector3.Dot(colorDelta, colorDelta));
            accumulated += sampleColor * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            return center;
        }
        Vector3 filtered = accumulated / totalWeight;
        float sprayMix = sprayRadius /
            MathF.Max(strokeLength + sprayRadius, 0.0001f);
        float grain = 0.75f + (0.25f * Hash(x, y, seed));
        float strength = (0.58f + (0.22f * sprayMix)) * grain;
        return Associated(
            Vector3.Lerp(centerColor, filtered, strength),
            center.W);
    }

    internal static Vector4 Crosshatch(
        PrismCatalogFilterPlan plan,
        Vector4 source,
        int x,
        int y)
    {
        if (source.W <= 0)
        {
            return Vector4.Zero;
        }

        float strength = Math.Clamp(
            plan.Options2.X,
            0,
            1);
        if (strength <= 0)
        {
            return source;
        }

        Vector3 straight = Vector3.Clamp(
            Unpremultiply(source),
            Vector3.Zero,
            Vector3.One);
        float darkness = 1 - StraightLuminance(straight);
        float period = MathF.Max(
            MathF.Abs(plan.Options0.X),
            4);
        float sharpness = Math.Clamp(
            plan.Options1.X / 10,
            0,
            1);
        float halfWidth = Math.Clamp(period * 0.075f, 0.45f, 1.4f);
        float spatialSoftness = 1.5f - (1.35f * sharpness);
        float toneSoftness = 0.18f - (0.16f * sharpness);
        float pixelX = x + 0.5f;
        float pixelY = y + 0.5f;
        float rising = (pixelX + pixelY) * 0.70710678118f;
        float falling = (pixelX - pixelY) * 0.70710678118f;
        float phaseStep = period / 3;

        float clear = 1;
        clear *= 1 - CrosshatchTransition(
            rising,
            period,
            0,
            halfWidth,
            spatialSoftness,
            darkness,
            0.06f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            falling,
            period,
            0,
            halfWidth,
            spatialSoftness,
            darkness,
            0.22f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            rising,
            period,
            phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.38f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            falling,
            period,
            phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.54f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            rising,
            period,
            2 * phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.70f,
            toneSoftness);
        clear *= 1 - CrosshatchTransition(
            falling,
            period,
            2 * phaseStep,
            halfWidth,
            spatialSoftness,
            darkness,
            0.86f,
            toneSoftness);

        Vector3 hatch = new(Math.Clamp(clear, 0, 1));
        return Associated(
            Vector3.Lerp(straight, hatch, strength),
            source.W);
    }

    private static float CrosshatchTransition(
        float coordinate,
        float period,
        float phase,
        float halfWidth,
        float spatialSoftness,
        float darkness,
        float toneThreshold,
        float toneSoftness)
    {
        float cycle =
            ((coordinate + phase) / period) -
            MathF.Floor((coordinate + phase) / period);
        float distance = MathF.Abs(cycle - 0.5f) * period;
        float line = 1 - SmoothStep(
            MathF.Max(halfWidth - spatialSoftness, 0),
            halfWidth + spatialSoftness,
            distance);
        float tone = SmoothStep(
            toneThreshold - toneSoftness,
            toneThreshold + toneSoftness,
            darkness);
        return line * tone;
    }
}
