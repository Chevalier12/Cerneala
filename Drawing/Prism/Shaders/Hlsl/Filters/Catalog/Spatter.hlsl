uint CatalogBlueNoiseHash(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float4 CatalogBlueNoisePoint(int2 cell, int layer)
{
    const int gridSize = 512;
    const int textureWidth = 1024;
    int2 wrapped = cell & (gridSize - 1);
    float2 pointUv = float2(
        (wrapped.x + 0.5 + (layer * gridSize)) / textureWidth,
        (wrapped.y + 0.5) / gridSize);
    return tex2Dlod(
        SpatterPointSampler,
        float4(pointUv, 0.0, 0.0));
}

float4 CatalogAssociatedSourceSample(float2 uv, int profile)
{
    float2 sampleUv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return WorkingAssociatedToLinearSrgb(
        tex2Dlod(
            SpriteTextureSampler,
            float4(sampleUv, 0.0, 0.0)),
        profile);
}

float4 CatalogSpatter(
    float2 uv,
    float4 source,
    int profile)
{
    float sprayRadius = max(FilterOptions2.x, 0.0);
    if (source.a <= 0.0 || sprayRadius <= 0.0)
    {
        return source;
    }

    uint seed =
        (uint(FilterOptions3.x) & 0xffffu) |
        ((uint(FilterOptions3.y) & 0xffffu) << 16);
    float2 seedOffset = float2(
        CatalogBlueNoiseHash(seed ^ 0x68bc21ebu) % 512u,
        CatalogBlueNoiseHash(seed ^ 0x02e5be93u) % 512u);
    float cellSize = sprayRadius * 0.5;
    float2 pattern =
        ((uv / PixelSize) / cellSize) + seedOffset;
    int2 baseCell = (int2)floor(pattern);
    float smoothness = saturate(FilterOptions1.x / 15.0);
    float edgeWidth = 0.04 + (0.24 * smoothness);
    float bestCoverage = 0.0;
    float3 bestColor = 1.0;

    [loop]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [loop]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int2 cell = baseCell + int2(offsetX, offsetY);
            [loop]
            for (int layer = 0; layer < 2; layer++)
            {
                float4 samplePoint = CatalogBlueNoisePoint(cell, layer);
                if (samplePoint.a <= 0.0)
                {
                    continue;
                }

                float2 pointPosition = float2(cell) + samplePoint.xy;
                float2 pointPixel =
                    (pointPosition - seedOffset) * cellSize;
                float2 pointUv = pointPixel * PixelSize;
                float4 pointSource = CatalogAssociatedSourceSample(
                    pointUv,
                    profile);
                float density =
                    (1.0 - CatalogLuminance(pointSource)) *
                    pointSource.a;
                if (samplePoint.z > density)
                {
                    continue;
                }

                float variation = frac(
                    (samplePoint.x * 37.0) +
                    (samplePoint.y * 91.0) +
                    (samplePoint.z * 65521.0));
                float pointRadius = 0.68 + (0.2 * variation);
                float distanceToPoint = distance(
                    pattern,
                    pointPosition);
                float coverage = 1.0 - smoothstep(
                    pointRadius - edgeWidth,
                    pointRadius + edgeWidth,
                    distanceToPoint);
                if (coverage > bestCoverage)
                {
                    bestCoverage = coverage;
                    bestColor = saturate(Unpremultiply(pointSource));
                }
            }
        }
    }

    float3 result = lerp(1.0, bestColor, bestCoverage);
    return float4(result * source.a, source.a);
}
