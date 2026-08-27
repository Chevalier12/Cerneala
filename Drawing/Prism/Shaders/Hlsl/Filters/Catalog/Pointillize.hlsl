float4 CatalogPointillize(
    float2 pixel,
    float cellSize,
    int profile)
{
    uint seed = CatalogPointillizeSeed();
    int2 cellIndex = (int2)floor(pixel / cellSize);
    float bestScore = 3.402823466e+38;
    float2 bestCenter = 0.0;
    float bestRadius = 0.0;

    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int2 candidateCell =
                cellIndex + int2(offsetX, offsetY);
            float2 candidateCenter =
                CatalogPointillizeCenter(
                    candidateCell,
                    seed,
                    cellSize);
            float4 candidateSample = CatalogLinearSample(
                (candidateCenter + 0.5) * PixelSize,
                profile);
            float darkness = saturate(
                (1.0 - CatalogLuminance(candidateSample)) *
                candidateSample.a);
            float threshold =
                (CatalogPointillizeRank(
                    candidateCell,
                    seed) + 0.5) /
                256.0;
            if (threshold > darkness)
            {
                continue;
            }

            float radius =
                cellSize *
                (0.28 + (0.2 * sqrt(darkness)));
            float antialiasWidth = min(0.75, radius);
            float2 delta = pixel - candidateCenter;
            float distanceSquared = dot(delta, delta);
            float maximumDistance =
                radius + antialiasWidth;
            if (distanceSquared >
                maximumDistance * maximumDistance)
            {
                continue;
            }

            float score =
                distanceSquared / (radius * radius);
            if (score < bestScore)
            {
                bestScore = score;
                bestCenter = candidateCenter;
                bestRadius = radius;
            }
        }
    }

    float4 background = FilterOptions1;
    background = float4(
        background.rgb * background.a,
        background.a);
    if (bestScore == 3.402823466e+38)
    {
        return background;
    }

    float antialiasWidth = min(0.75, bestRadius);
    float coverage = 1.0 - smoothstep(
        bestRadius - antialiasWidth,
        bestRadius + antialiasWidth,
        distance(pixel, bestCenter));
    return lerp(
        background,
        CatalogPointillizeAverage(
            bestCenter,
            bestRadius,
            profile),
        coverage);
}
