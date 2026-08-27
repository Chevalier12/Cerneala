



uint ReticulationHash(int2 cell, uint seed)
{
    uint value =
        (uint(cell.x) * 0x9e3779b9u) ^
        (uint(cell.y) * 0x85ebca6bu) ^
        seed;
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float ReticulationRandom(int2 cell, uint seed)
{
    return float(ReticulationHash(cell, seed) & 0x00ffffffu) /
        16777215.0;
}

float2 ReticulationFeature(int2 cell, uint seed)
{
    return float2(cell) + 0.15 +
        (0.7 * float2(
            ReticulationRandom(cell, seed ^ 0x13579bdfu),
            ReticulationRandom(cell, seed ^ 0x2468ace0u)));
}

float ReticulationCellularGap(float2 pixel, float cellSize, uint seed)
{
    float2 pattern = pixel / cellSize;
    int2 baseCell = (int2)floor(pattern);
    float nearest = 3.402823466e+38;
    float secondNearest = 3.402823466e+38;

    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int2 cell = baseCell + int2(offsetX, offsetY);
            float2 delta = pattern - ReticulationFeature(cell, seed);
            float distanceSquared = dot(delta, delta);
            if (distanceSquared < nearest)
            {
                secondNearest = nearest;
                nearest = distanceSquared;
            }
            else if (distanceSquared < secondNearest)
            {
                secondNearest = distanceSquared;
            }
        }
    }

    return sqrt(secondNearest) - sqrt(nearest);
}

float4 CatalogReticulation(float2 pixel, float4 source)
{
    const float minimumAlpha = 0.000001;
    if (source.a <= minimumAlpha)
    {
        return 0.0;
    }

    float cellSize = clamp(FilterOptions4.x, 2.0, 256.0);
    float foregroundLevel = saturate(FilterOptions4.y);
    float backgroundLevel = saturate(FilterOptions4.z);
    uint seed =
        (uint(FilterOptions3.x) & 0xffffu) |
        ((uint(FilterOptions3.y) & 0xffffu) << 16);
    float3 straight = saturate(Unpremultiply(source));
    float luminance = dot(straight, float3(0.2126, 0.7152, 0.0722));
    float gap = ReticulationCellularGap(pixel, cellSize, seed);
    float shadowWeight = sqrt(1.0 - luminance);
    float highlightWidth = lerp(0.018, 0.075, backgroundLevel);
    float shadowWidth = lerp(0.055, 0.24, foregroundLevel);
    float ridgeWidth = lerp(
        highlightWidth,
        shadowWidth,
        shadowWeight);
    float ridge = 1.0 - smoothstep(0.0, ridgeWidth, gap);
    float inkStrength = lerp(
        backgroundLevel * 0.38,
        foregroundLevel * 0.95,
        shadowWeight);
    float paper = lerp(
        luminance,
        1.0,
        backgroundLevel * 0.1 * sqrt(luminance));
    float outputLuminance = saturate(
        paper * (1.0 - (ridge * inkStrength)));
    float3 output = luminance <= minimumAlpha
        ? outputLuminance
        : saturate(straight * (outputLuminance / luminance));
    return float4(output * source.a, source.a);
}
