float4 CatalogMosaicBilateral(
    float2 pixel,
    float2 cell,
    int profile)
{
    const float inverseTwoRangeSigmaSquared = 8.0;
    float2 cellCenter =
        (floor(pixel / cell) + 0.5) * cell;
    float4 reference = CatalogLinearSample(
        cellCenter * PixelSize,
        profile);
    float3 referenceStraight =
        reference.a <= 0.000001
            ? 0.0
            : Unpremultiply(reference);
    float4 weighted = 0.0;
    float totalWeight = 0.0;
    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            float2 offset = float2(offsetX, offsetY);
            float4 sample =
                offsetX == 0 && offsetY == 0
                    ? reference
                    : CatalogLinearSample(
                        (cellCenter + (offset * cell / 3.0)) *
                            PixelSize,
                        profile);
            float3 sampleStraight =
                sample.a <= 0.000001
                    ? 0.0
                    : Unpremultiply(sample);
            float3 colorDelta =
                sampleStraight - referenceStraight;
            float alphaDelta = sample.a - reference.a;
            float rangeDistanceSquared =
                dot(colorDelta, colorDelta) +
                (alphaDelta * alphaDelta);
            float spatialWeight =
                exp(-0.5 * dot(offset, offset));
            float rangeWeight = exp(
                -rangeDistanceSquared *
                inverseTwoRangeSigmaSquared);
            float weight = spatialWeight * rangeWeight;
            weighted += sample * weight;
            totalWeight += weight;
        }
    }

    float4 result = weighted / max(totalWeight, 0.000001);
    result.a = saturate(result.a);
    result.rgb = clamp(result.rgb, 0.0, result.a);
    return result;
}

float4 CatalogMosaic(float2 pixel, int profile)
{
    float2 cell = max(FilterOptions0.xy, float2(1.0, 1.0));
    if (FilterOptions1.x >= 0.5)
    {
        return CatalogMosaicBilateral(pixel, cell, profile);
    }

    float2 cellIndex = floor(pixel / cell);
    float2 center = (cellIndex + 0.5) * cell;
    return CatalogLinearSample(center * PixelSize, profile);
}
