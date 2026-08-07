



uint CraquelureHash(int2 cell, uint seed)
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

float CraquelureRandom(int2 cell, uint seed)
{
    return float(CraquelureHash(cell, seed) & 0x00ffffffu) /
        16777215.0;
}

float2 CraquelureGradient(int2 cell, uint seed)
{
    const float diagonal = 0.70710678118;
    uint direction = CraquelureHash(cell, seed) & 7u;
    if (direction == 0u)
    {
        return float2(1.0, 0.0);
    }
    if (direction == 1u)
    {
        return float2(-1.0, 0.0);
    }
    if (direction == 2u)
    {
        return float2(0.0, 1.0);
    }
    if (direction == 3u)
    {
        return float2(0.0, -1.0);
    }
    if (direction == 4u)
    {
        return float2(diagonal, diagonal);
    }
    if (direction == 5u)
    {
        return float2(-diagonal, diagonal);
    }
    if (direction == 6u)
    {
        return float2(diagonal, -diagonal);
    }
    return float2(-diagonal, -diagonal);
}

float CraquelureFade(float value)
{
    return value * value * value *
        ((value * ((value * 6.0) - 15.0)) + 10.0);
}

float CraquelureGradientNoise(float2 position, uint seed)
{
    int2 cell = (int2)floor(position);
    float2 local = position - float2(cell);
    float2 fade = float2(
        CraquelureFade(local.x),
        CraquelureFade(local.y));
    float upper = lerp(
        dot(
            CraquelureGradient(cell, seed),
            local),
        dot(
            CraquelureGradient(cell + int2(1, 0), seed),
            local - float2(1.0, 0.0)),
        fade.x);
    float lower = lerp(
        dot(
            CraquelureGradient(cell + int2(0, 1), seed),
            local - float2(0.0, 1.0)),
        dot(
            CraquelureGradient(cell + int2(1, 1), seed),
            local - float2(1.0, 1.0)),
        fade.x);
    return lerp(upper, lower, fade.y) * 1.41421356237;
}

float2 CraquelureDomainWarp(float2 pattern, uint seed)
{
    float2 position = pattern * 0.55;
    return float2(
        CraquelureGradientNoise(
            position + float2(19.1, 7.7),
            seed ^ 0x68bc21ebu),
        CraquelureGradientNoise(
            position + float2(-5.4, 23.6),
            seed ^ 0x02e5be93u)) * 0.28;
}

float2 CraquelureFeature(int2 cell, uint seed)
{
    return float2(cell) + 0.5 +
        (0.85 * (float2(
            CraquelureRandom(cell, seed ^ 0x13579bdfu),
            CraquelureRandom(cell, seed ^ 0x2468ace0u)) - 0.5));
}

float CraquelureVoronoiEdgeDistance(float2 pattern, uint seed)
{
    int2 baseCell = (int2)floor(pattern);
    float nearestDistanceSquared = 3.402823466e+38;
    float2 nearest = 0.0;

    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            float2 relative = CraquelureFeature(
                baseCell + int2(offsetX, offsetY),
                seed) - pattern;
            float distanceSquared = dot(relative, relative);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearest = relative;
            }
        }
    }

    float edgeDistance = 3.402823466e+38;
    [unroll]
    for (int edgeY = -2; edgeY <= 2; edgeY++)
    {
        [unroll]
        for (int edgeX = -2; edgeX <= 2; edgeX++)
        {
            float2 relative = CraquelureFeature(
                baseCell + int2(edgeX, edgeY),
                seed) - pattern;
            float2 between = relative - nearest;
            float lengthSquared = dot(between, between);
            if (lengthSquared > 0.00001)
            {
                float2 normal = between * rsqrt(lengthSquared);
                edgeDistance = min(
                    edgeDistance,
                    dot((nearest + relative) * 0.5, normal));
            }
        }
    }

    return max(edgeDistance, 0.0);
}

float4 CatalogCraquelure(float2 pixel, float4 source)
{
    const float minimumAlpha = 0.000001;
    if (source.a <= minimumAlpha)
    {
        return 0.0;
    }

    float cellSize = clamp(FilterOptions4.x, 2.0, 256.0);
    float crackWidth = clamp(FilterOptions4.y, 0.01, 0.25);
    float depth = saturate(FilterOptions4.z);
    float brightness = saturate(FilterOptions4.w);
    uint seed =
        (uint(FilterOptions3.x) & 0xffffu) |
        ((uint(FilterOptions3.y) & 0xffffu) << 16);
    float2 pattern = pixel / cellSize;
    pattern += CraquelureDomainWarp(pattern, seed);
    float edgeDistance = CraquelureVoronoiEdgeDistance(pattern, seed);
    float antialias = clamp(0.75 / cellSize, 0.0025, 0.25);
    float smoothness = crackWidth * 0.35;
    float crack = 1.0 - smoothstep(
        max(crackWidth - smoothness, 0.0),
        crackWidth + antialias,
        edgeDistance);
    float rimNear = crackWidth + (antialias * 0.25);
    float rimPeak = crackWidth + max(0.012, antialias * 1.25);
    float rimFar = crackWidth + max(0.06, antialias * 3.0);
    float rim = smoothstep(rimNear, rimPeak, edgeDistance) *
        (1.0 - smoothstep(rimPeak, rimFar, edgeDistance));
    float shadowStrength =
        (0.18 + (0.7 * depth)) *
        (0.55 + (0.45 * brightness));
    float highlightStrength =
        (0.02 + (0.18 * brightness)) *
        (0.4 + (0.6 * depth));
    float3 straight = saturate(Unpremultiply(source));
    float3 output = saturate(
        (straight * (1.0 - (crack * shadowStrength))) +
        (rim * highlightStrength));
    return float4(output * source.a, source.a);
}
