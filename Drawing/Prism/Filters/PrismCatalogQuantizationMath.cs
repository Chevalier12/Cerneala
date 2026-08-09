using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogQuantizationMath
{
    internal static Vector4 BilateralMosaic(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float cellX,
        float cellY)
    {
        const float inverseTwoRangeSigmaSquared = 8;
        float centerX =
            (MathF.Floor((x + 0.5f) / cellX) + 0.5f) * cellX;
        float centerY =
            (MathF.Floor((y + 0.5f) / cellY) + 0.5f) * cellY;
        Vector4 reference = SamplePixelBilinear(
            source,
            width,
            height,
            centerX - 0.5f,
            centerY - 0.5f);
        Vector3 referenceStraight = Unpremultiply(reference);
        Vector4 weighted = Vector4.Zero;
        float totalWeight = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                Vector4 sample =
                    offsetX == 0 && offsetY == 0
                        ? reference
                        : SamplePixelBilinear(
                            source,
                            width,
                            height,
                            centerX +
                                (offsetX * cellX / 3) -
                                0.5f,
                            centerY +
                                (offsetY * cellY / 3) -
                                0.5f);
                Vector3 colorDelta =
                    Unpremultiply(sample) - referenceStraight;
                float alphaDelta = sample.W - reference.W;
                float rangeDistanceSquared =
                    Vector3.Dot(colorDelta, colorDelta) +
                    (alphaDelta * alphaDelta);
                float spatialWeight = MathF.Exp(
                    -0.5f *
                    ((offsetX * offsetX) +
                        (offsetY * offsetY)));
                float rangeWeight = MathF.Exp(
                    -rangeDistanceSquared *
                    inverseTwoRangeSigmaSquared);
                float weight = spatialWeight * rangeWeight;
                weighted += sample * weight;
                totalWeight += weight;
            }
        }

        return weighted / totalWeight;
    }

    internal static Vector4 AnisotropicKuwahara(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        const int radius = 3;
        const float alpha = 1;
        const float zeta = 2f / radius;
        const float gamma = 3 * MathF.PI / 16;
        const float diagonal = 0.7071067811865476f;
        float eta =
            (zeta + MathF.Cos(gamma)) /
            MathF.Pow(MathF.Sin(gamma), 2);
        Vector4 center = SamplePixel(
            source,
            width,
            height,
            x,
            y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(
                source,
                width,
                height,
                x,
                y);
        float discriminant = MathF.Sqrt(
            MathF.Max(
                0,
                ((tensorX - tensorY) *
                    (tensorX - tensorY)) +
                (4 * tensorCross * tensorCross)));
        float lambda1 =
            0.5f * (tensorX + tensorY + discriminant);
        float lambda2 =
            0.5f * (tensorX + tensorY - discriminant);
        float anisotropy =
            (lambda1 + lambda2) <= 0.000001f
                ? 0
                : Math.Clamp(
                    (lambda1 - lambda2) /
                        (lambda1 + lambda2),
                    0,
                    1);
        float angle =
            (0.5f * MathF.Atan2(
                2 * tensorCross,
                tensorX - tensorY)) +
            (MathF.PI * 0.5f);
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float majorRadius =
            radius * ((alpha + anisotropy) / alpha);
        float minorRadius =
            radius * (alpha / (alpha + anisotropy));
        int sampleRadius =
            (int)MathF.Ceiling(majorRadius);

        Span<Vector3> colorSums = stackalloc Vector3[8];
        Span<Vector3> squareSums = stackalloc Vector3[8];
        Span<float> weightSums = stackalloc float[8];
        Span<float> sectorWeights = stackalloc float[8];
        for (int offsetY = -sampleRadius;
            offsetY <= sampleRadius;
            offsetY++)
        {
            for (int offsetX = -sampleRadius;
                offsetX <= sampleRadius;
                offsetX++)
            {
                float localX =
                    ((cosine * offsetX) +
                        (sine * offsetY)) /
                    majorRadius;
                float localY =
                    ((-sine * offsetX) +
                        (cosine * offsetY)) /
                    minorRadius;
                if ((localX * localX) +
                        (localY * localY) >
                    1)
                {
                    continue;
                }

                FacetSectorWeights(
                    localX,
                    localY,
                    zeta,
                    eta,
                    diagonal,
                    sectorWeights);
                float sectorTotal = 0;
                for (int sector = 0; sector < 8; sector++)
                {
                    sectorTotal += sectorWeights[sector];
                }
                if (sectorTotal <= 0.000001f)
                {
                    continue;
                }

                float gaussian =
                    MathF.Exp(
                        -3.125f *
                        ((localX * localX) +
                            (localY * localY))) /
                    sectorTotal;
                Vector4 sample = SamplePixel(
                    source,
                    width,
                    height,
                    x + offsetX,
                    y + offsetY);
                if (sample.W <= 0)
                {
                    continue;
                }
                Vector3 straight = Vector3.Clamp(
                    Unpremultiply(sample),
                    Vector3.Zero,
                    Vector3.One);
                for (int sector = 0; sector < 8; sector++)
                {
                    float weight =
                        sectorWeights[sector] *
                        gaussian *
                        sample.W;
                    colorSums[sector] += straight * weight;
                    squareSums[sector] +=
                        straight * straight * weight;
                    weightSums[sector] += weight;
                }
            }
        }

        Vector3 result = Vector3.Zero;
        float resultWeight = 0;
        Vector3 centerStraight = Unpremultiply(center);
        for (int sector = 0; sector < 8; sector++)
        {
            if (weightSums[sector] <= 0.000001f)
            {
                continue;
            }
            Vector3 mean =
                colorSums[sector] / weightSums[sector];
            Vector3 variance = Vector3.Max(
                Vector3.Zero,
                (squareSums[sector] /
                    weightSums[sector]) -
                (mean * mean));
            float varianceSum =
                variance.X + variance.Y + variance.Z;
            float confidence =
                1 /
                (1 +
                    MathF.Pow(
                        1000 * varianceSum,
                        4));
            result += mean * confidence;
            resultWeight += confidence;
        }
        if (resultWeight <= 0.000001f)
        {
            result = centerStraight;
        }
        else
        {
            result /= resultWeight;
        }
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static (
        float Horizontal,
        float Cross,
        float Vertical) FacetStructureTensor(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector3 topLeft = FacetStraightSample(
            source,
            width,
            height,
            x - 1,
            y - 1);
        Vector3 top = FacetStraightSample(
            source,
            width,
            height,
            x,
            y - 1);
        Vector3 topRight = FacetStraightSample(
            source,
            width,
            height,
            x + 1,
            y - 1);
        Vector3 left = FacetStraightSample(
            source,
            width,
            height,
            x - 1,
            y);
        Vector3 right = FacetStraightSample(
            source,
            width,
            height,
            x + 1,
            y);
        Vector3 bottomLeft = FacetStraightSample(
            source,
            width,
            height,
            x - 1,
            y + 1);
        Vector3 bottom = FacetStraightSample(
            source,
            width,
            height,
            x,
            y + 1);
        Vector3 bottomRight = FacetStraightSample(
            source,
            width,
            height,
            x + 1,
            y + 1);
        Vector3 horizontal =
            -topLeft + topRight -
            (2 * left) + (2 * right) -
            bottomLeft + bottomRight;
        Vector3 vertical =
            -topLeft - (2 * top) - topRight +
            bottomLeft + (2 * bottom) + bottomRight;
        return (
            Vector3.Dot(horizontal, horizontal),
            Vector3.Dot(horizontal, vertical),
            Vector3.Dot(vertical, vertical));
    }

    private static Vector3 FacetStraightSample(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 sample = SamplePixel(
            source,
            width,
            height,
            x,
            y);
        return sample.W <= 0
            ? Vector3.Zero
            : Vector3.Clamp(
                Unpremultiply(sample),
                Vector3.Zero,
                Vector3.One);
    }

    internal static void FacetSectorWeights(
        float x,
        float y,
        float zeta,
        float eta,
        float diagonal,
        Span<float> weights)
    {
        FacetCardinalSectorWeights(
            x,
            y,
            zeta,
            eta,
            weights,
            0);
        float rotatedX = diagonal * (x - y);
        float rotatedY = diagonal * (x + y);
        FacetCardinalSectorWeights(
            rotatedX,
            rotatedY,
            zeta,
            eta,
            weights,
            1);
    }

    private static void FacetCardinalSectorWeights(
        float x,
        float y,
        float zeta,
        float eta,
        Span<float> weights,
        int start)
    {
        float xPolynomial = zeta - (eta * x * x);
        float yPolynomial = zeta - (eta * y * y);
        weights[start] = FacetSquaredPositive(y + xPolynomial);
        weights[start + 2] =
            FacetSquaredPositive(-x + yPolynomial);
        weights[start + 4] =
            FacetSquaredPositive(-y + xPolynomial);
        weights[start + 6] =
            FacetSquaredPositive(x + yPolynomial);
    }

    private static float FacetSquaredPositive(float value)
    {
        float positive = MathF.Max(0, value);
        return positive * positive;
    }

    internal static Vector4 ColorHalftone(
        PrismCatalogFilterPlan plan,
        Vector4 source,
        Vector2 pixel,
        float maxRadius)
    {
        if (maxRadius <= 0)
        {
            return source;
        }

        Vector3 straight = Vector3.Clamp(
            Unpremultiply(source),
            Vector3.Zero,
            Vector3.One);
        float black = 1 - MathF.Max(
            straight.X,
            MathF.Max(straight.Y, straight.Z));
        float colorRange = 1 - black;
        Vector3 cmy = colorRange <= 0.000001f
            ? Vector3.Zero
            : Vector3.Clamp(
                (Vector3.One - straight - new Vector3(black)) /
                    colorRange,
                Vector3.Zero,
                Vector3.One);
        Vector4 angles = OptionVector(
            plan,
            "Angles",
            new Vector4(108, 162, 90, 45));
        float cyanInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.X,
            cmy.X);
        float magentaInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.Y,
            cmy.Y);
        float yellowInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.Z,
            cmy.Z);
        float blackInk = ColorHalftoneInk(
            pixel,
            maxRadius,
            angles.W,
            black);
        float blackPaper = 1 - blackInk;
        return Associated(
            new Vector3(
                (1 - cyanInk) * blackPaper,
                (1 - magentaInk) * blackPaper,
                (1 - yellowInk) * blackPaper),
            source.W);
    }

    private static float ColorHalftoneInk(
        Vector2 pixel,
        float maxRadius,
        float angleDegrees,
        float coverage)
    {
        if (coverage <= 0)
        {
            return 0;
        }
        if (coverage >= 1)
        {
            return 1;
        }

        float threshold = ColorHalftoneThreshold(
            pixel,
            maxRadius,
            angleDegrees);
        float antialiasWidth = Math.Clamp(
            0.5f / maxRadius,
            0.0001f,
            0.25f);
        return SmoothStep(
            threshold - antialiasWidth,
            threshold + antialiasWidth,
            coverage);
    }

    private static float ColorHalftoneThreshold(
        Vector2 pixel,
        float maxRadius,
        float angleDegrees)
    {
        float radians = angleDegrees * (MathF.PI / 180);
        float cosine = MathF.Cos(radians);
        float sine = MathF.Sin(radians);
        Vector2 rotated = new(
            (cosine * pixel.X) - (sine * pixel.Y),
            (sine * pixel.X) + (cosine * pixel.Y));
        float cellSize = MathF.Sqrt(2) * maxRadius;
        Vector2 local = rotated - new Vector2(
            MathF.Floor((rotated.X / cellSize) + 0.5f) * cellSize,
            MathF.Floor((rotated.Y / cellSize) + 0.5f) * cellSize);
        float halfCell = cellSize * 0.5f;
        float normalizedSquaredDistance = Math.Clamp(
            Vector2.Dot(local, local) / (halfCell * halfCell),
            0,
            2);
        float circleCoverage = MathF.PI * 0.25f;
        if (normalizedSquaredDistance <= 1)
        {
            return circleCoverage * normalizedSquaredDistance;
        }

        return circleCoverage +
            ((1 - circleCoverage) *
                (normalizedSquaredDistance - 1));
    }
}
