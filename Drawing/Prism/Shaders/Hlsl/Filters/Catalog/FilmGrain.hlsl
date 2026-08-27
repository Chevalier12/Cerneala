float CatalogFilmGrainGaussian(
    int2 position,
    uint seed)
{
    return (
        DryBrushHash(position, seed ^ 0xa511e9b3u) +
        DryBrushHash(position, seed ^ 0x63d83595u) +
        DryBrushHash(position, seed ^ 0xb8d26d4du) +
        DryBrushHash(position, seed ^ 0x9e3779b9u) -
        2.0) *
        1.7320508075688772;
}

float4 CatalogFilmGrain(
    float2 uv,
    float4 source)
{
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    float intensity =
        clamp(FilterOptions2.x, 0.0, 10.0) *
        0.01;
    if (intensity <= 0.0)
    {
        return source;
    }

    float grain = clamp(
        FilterOptions0.x,
        0.0,
        20.0);
    float grainScale = 1.0 + (grain * 0.25);
    float sigma = grainScale * 0.55;
    float inverseTwoSigmaSquared =
        0.5 / (sigma * sigma);
    float2 pixel = uv / PixelSize;
    int2 cell = (int2)floor(pixel / grainScale);
    uint seed =
        (uint(FilterOptions3.x) & 0xffffu) |
        ((uint(FilterOptions3.y) & 0xffffu) << 16);
    float weightedNoise = 0.0;
    float squaredWeightTotal = 0.0;
    [loop]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [loop]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int2 node = cell + int2(offsetX, offsetY);
            float2 nodePosition =
                (float2(node) + 0.5) * grainScale;
            float2 delta = pixel - nodePosition;
            float weight = exp(
                -dot(delta, delta) *
                inverseTwoSigmaSquared);
            weightedNoise +=
                CatalogFilmGrainGaussian(node, seed) *
                weight;
            squaredWeightTotal += weight * weight;
        }
    }

    float correlatedNoise = weightedNoise /
        sqrt(max(squaredWeightTotal, 0.000001));
    float3 straight = saturate(Unpremultiply(source));
    float luminance = CatalogLuminance(source);
    float highlightArea =
        clamp(FilterOptions1.x, 0.0, 20.0) /
        20.0;
    float variancePeak = 0.5 +
        (highlightArea * 0.4);
    float booleanLevel = luminance <= variancePeak
        ? 0.5 * luminance / variancePeak
        : 0.5 +
            (0.5 *
                (luminance - variancePeak) /
                (1.0 - variancePeak));
    float signalDeviation = 2.0 * sqrt(max(
        booleanLevel * (1.0 - booleanLevel),
        0.0));
    float3 result = saturate(
        straight +
        (correlatedNoise *
            intensity *
            signalDeviation));
    return float4(result * source.a, source.a);
}
