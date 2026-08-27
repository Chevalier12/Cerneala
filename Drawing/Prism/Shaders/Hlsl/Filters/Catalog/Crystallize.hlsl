float4 CatalogCrystallize(
    float2 pixel,
    float cell,
    int profile)
{
    int2 cellIndex = (int2)floor(pixel / cell);
    uint seed = CatalogQuantizationSeed();
    float2 nearest = 0.0;
    float nearestDistanceSquared = 3.402823466e+38;
    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int2 candidateCell =
                cellIndex + int2(offsetX, offsetY);
            float2 candidate =
                ((float2)candidateCell +
                    float2(
                        CatalogCrystallizeHash(
                            candidateCell,
                            seed),
                        CatalogCrystallizeHash(
                            candidateCell,
                            seed + 1u))) *
                cell;
            float2 delta = pixel - candidate;
            float distanceSquared = dot(delta, delta);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearest = candidate;
            }
        }
    }
    float2 samplePixel = round(nearest);
    return CatalogLinearSample(
        (samplePixel + 0.5) * PixelSize,
        profile);
}
