using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogColorMath
{
    internal static Vector4 Convolution(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        Func<Vector2, Vector4>? kernel)
    {
        return PrismCustomConvolutionFilter.ApplyPixel(
            plan, source, width, height, x, y, kernel);
    }

    internal static Vector4 Color(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        PrismColorMatrixResource? colorMatrixResource)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        Vector3 straight = Unpremultiply(center);
        if (plan.Filter == PrismFilterId.Solarize)
        {
            straight = PrismSolarizeFilter.Apply(
                straight,
                Option(plan, "Threshold", 0.5f));
        }
        else if (plan.Filter == PrismFilterId.Color)
        {
            straight = PrismColorFilter.Apply(
                straight,
                Option(plan, "Brightness", 0),
                Option(plan, "Contrast", 1),
                Option(plan, "Exposure", 0),
                Option(plan, "Saturation", 1),
                Option(plan, "Hue", 0),
                Option(plan, "Temperature", 0),
                OptionVector(plan, "Tint", Vector4.Zero),
                Option(plan, "Clamp", 1) >= 0.5f);
        }
        else if (plan.Filter == PrismFilterId.ColorMatrix)
        {
            Vector4 transformed = PrismColorMatrixFilter.Apply(
                new Vector4(straight, center.W),
                colorMatrixResource,
                Option(plan, "Clamp", 1) >= 0.5f);
            return Associated(
                new Vector3(
                    transformed.X,
                    transformed.Y,
                    transformed.Z),
                transformed.W);
        }

        bool clamp = Option(plan, "Clamp", 1) >= 0.5f;
        if (clamp)
        {
            straight = Vector3.Clamp(
                straight,
                Vector3.Zero,
                Vector3.One);
        }
        return Associated(straight, center.W);
    }

    internal static float ParameterMagnitude(
        PrismCatalogFilterPlan plan)
    {
        float total = 0;
        for (int index = 0; index < 9; index++)
        {
            Vector4 option = plan.GetOption(index);
            total += MathF.Abs(option.X) +
                MathF.Abs(option.Y) +
                MathF.Abs(option.Z) +
                MathF.Abs(option.W);
        }
        return total;
    }

    internal static Vector4 Pointillize(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        float cellSize,
        uint seed,
        Vector4 background)
    {
        int cellX = (int)MathF.Floor(x / cellSize);
        int cellY = (int)MathF.Floor(y / cellSize);
        float bestScore = float.PositiveInfinity;
        Vector2 bestCenter = default;
        float bestRadius = 0;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int candidateCellX = cellX + offsetX;
                int candidateCellY = cellY + offsetY;
                Vector2 candidateCenter =
                    PrismIncrementalVoronoiSet.Center(
                        candidateCellX,
                        candidateCellY,
                        seed,
                        cellSize);
                Vector4 candidateSample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    candidateCenter.X,
                    candidateCenter.Y);
                float darkness = Math.Clamp(
                    (1 - Luminance(candidateSample)) *
                        candidateSample.W,
                    0,
                    1);
                if (PrismIncrementalVoronoiSet.Threshold(
                        candidateCellX,
                        candidateCellY,
                        seed) >
                    darkness)
                {
                    continue;
                }

                float radius = cellSize *
                    (0.28f + (0.2f * MathF.Sqrt(darkness)));
                float antialiasWidth = MathF.Min(0.75f, radius);
                Vector2 delta =
                    new(x - candidateCenter.X, y - candidateCenter.Y);
                float distanceSquared = delta.LengthSquared();
                float maximumDistance = radius + antialiasWidth;
                if (distanceSquared >
                    maximumDistance * maximumDistance)
                {
                    continue;
                }

                float score =
                    distanceSquared / (radius * radius);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestCenter = candidateCenter;
                bestRadius = radius;
            }
        }

        if (!float.IsFinite(bestScore))
        {
            return background;
        }

        float distance = Vector2.Distance(
            new Vector2(x, y),
            bestCenter);
        float antialias = MathF.Min(0.75f, bestRadius);
        float coverage = 1 - SmoothStep(
            bestRadius - antialias,
            bestRadius + antialias,
            distance);
        float sampleOffset = MathF.Min(
            bestRadius * 0.35f,
            1.5f);
        Vector4 dotColor = (
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X,
                bestCenter.Y) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X - sampleOffset,
                bestCenter.Y) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X + sampleOffset,
                bestCenter.Y) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X,
                bestCenter.Y - sampleOffset) +
            SamplePixelBilinear(
                source,
                width,
                height,
                bestCenter.X,
                bestCenter.Y + sampleOffset)) / 5;
        return ClampAssociated(
            Vector4.Lerp(
                background,
                dotColor,
                coverage));
    }
}
