float2 CatalogSprayedStrokeDirection()
{
    int direction = (int)round(FilterOptions0.x);
    if (direction == 1)
    {
        return float2(1.0, 0.0);
    }
    if (direction == 2)
    {
        return normalize(float2(-1.0, 1.0));
    }
    if (direction == 3)
    {
        return float2(0.0, 1.0);
    }
    return normalize(float2(1.0, 1.0));
}

float CatalogSprayedStrokeHash(int2 cell, uint seed)
{
    uint value =
        (uint(cell.x) * 0x9e3779b9u) ^
        (uint(cell.y) * 0x85ebca6bu) ^
        seed;
    return
        (CatalogBlueNoiseHash(value) & 0x00ffffffu) /
        16777215.0;
}

float2 CatalogSprayedStrokeJitter(
    int2 cell,
    uint seed,
    uint salt)
{
    float4 samplePoint = CatalogBlueNoisePoint(cell, 0);
    if (samplePoint.a > 0.0)
    {
        return samplePoint.xy;
    }
    return float2(
        CatalogSprayedStrokeHash(cell, seed ^ salt),
        CatalogSprayedStrokeHash(
            cell,
            seed ^ salt ^ 0x85ebca6bu));
}

float4 CatalogSprayedStrokeTap(
    float2 pixel,
    float2 direction,
    float2 normal,
    float strokeLength,
    float sprayRadius,
    float position,
    float baseWeight,
    int2 fieldCell,
    uint seed,
    uint salt,
    float3 centerColor,
    int profile)
{
    float2 jitter = CatalogSprayedStrokeJitter(
        fieldCell,
        seed,
        salt);
    float longitudinal =
        (position * strokeLength) +
        ((jitter.x - 0.5) * strokeLength / 7.0);
    float lateral =
        (jitter.y - 0.5) * 2.0 * sprayRadius;
    float2 samplePixel =
        pixel +
        (direction * longitudinal) +
        (normal * lateral);
    float4 sample = CatalogAssociatedSourceSample(
        samplePixel * PixelSize,
        profile);
    if (sample.a <= 0.0)
    {
        return 0.0;
    }

    float3 sampleColor = saturate(Unpremultiply(sample));
    float3 colorDelta = sampleColor - centerColor;
    float weight = baseWeight /
        (1.0 + (3.0 * dot(colorDelta, colorDelta)));
    return float4(sampleColor * weight, weight);
}

float4 CatalogSprayedStrokes(
    float2 uv,
    float4 source,
    int profile)
{
    float strokeLength = max(FilterOptions3.x, 0.0);
    float sprayRadius = max(FilterOptions2.x, 0.0);
    if (source.a <= 0.0 ||
        (strokeLength <= 0.0 && sprayRadius <= 0.0))
    {
        return source;
    }

    uint seed =
        (uint(FilterOptions1.x) & 0xffffu) |
        ((uint(FilterOptions1.y) & 0xffffu) << 16);
    float2 seedOffset = float2(
        CatalogBlueNoiseHash(seed ^ 0x68bc21ebu) % 512u,
        CatalogBlueNoiseHash(seed ^ 0x02e5be93u) % 512u);
    float2 pixel = uv / PixelSize;
    float cellSize = max(sprayRadius, 2.0);
    int2 baseCell = (int2)floor(
        (pixel / cellSize) + seedOffset);
    float2 direction = CatalogSprayedStrokeDirection();
    float2 normal = float2(-direction.y, direction.x);
    float3 centerColor = saturate(Unpremultiply(source));
    float4 accumulated = 0.0;
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        -0.5, 0.08, baseCell + int2(-1, 0),
        seed, 0x9e3779b9u, centerColor, profile);
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        -0.33333334, 0.12, baseCell + int2(0, -1),
        seed, 0x3c6ef372u, centerColor, profile);
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        -0.16666667, 0.18, baseCell + int2(1, 0),
        seed, 0xdaa66d2bu, centerColor, profile);
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        0.0, 0.24, baseCell,
        seed, 0x78dde6e4u, centerColor, profile);
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        0.16666667, 0.18, baseCell + int2(0, 1),
        seed, 0x1715609du, centerColor, profile);
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        0.33333334, 0.12, baseCell + int2(-1, 1),
        seed, 0xb54cda56u, centerColor, profile);
    accumulated += CatalogSprayedStrokeTap(
        pixel, direction, normal, strokeLength, sprayRadius,
        0.5, 0.08, baseCell + int2(1, -1),
        seed, 0x5384540fu, centerColor, profile);
    if (accumulated.a <= 0.0)
    {
        return source;
    }

    float3 filtered = accumulated.rgb / accumulated.a;
    float sprayMix = sprayRadius /
        max(strokeLength + sprayRadius, 0.0001);
    float grain = 0.75 + (0.25 * CatalogSprayedStrokeHash(
        (int2)floor(pixel),
        seed));
    float strength =
        (0.58 + (0.22 * sprayMix)) * grain;
    float3 result = lerp(centerColor, filtered, strength);
    return float4(result * source.a, source.a);
}
