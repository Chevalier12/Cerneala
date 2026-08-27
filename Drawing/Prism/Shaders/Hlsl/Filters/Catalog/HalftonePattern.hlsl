float CatalogHalftoneDotThreshold(
    float2 pixel,
    float cellSize)
{
    float2 local = pixel -
        floor((pixel / cellSize) + 0.5) * cellSize;
    float radius = clamp(
        length(local) / (cellSize * 0.5),
        0.0,
        1.41421356237);
    if (radius <= 1.0)
    {
        return 0.78539816339 * radius * radius;
    }

    float radiusSquared = radius * radius;
    float outsideAxis = sqrt(max(radiusSquared - 1.0, 0.0));
    return saturate(
        outsideAxis +
        (radiusSquared *
            (asin(1.0 / radius) - 0.78539816339)));
}

float CatalogHalftoneLineThreshold(
    float2 pixel,
    float cellSize)
{
    float local = pixel.y -
        floor((pixel.y / cellSize) + 0.5) * cellSize;
    return saturate(abs(local) / (cellSize * 0.5));
}

float CatalogHalftoneCircleThreshold(
    float2 pixel,
    float cellSize)
{
    float2 imageCenter = 0.5 / PixelSize;
    float radius = distance(pixel, imageCenter);
    float ring = floor(radius / cellSize);
    float innerRadius = ring * cellSize;
    float outerRadius = innerRadius + cellSize;
    float areaPhase =
        ((radius * radius) -
            (innerRadius * innerRadius)) /
        ((outerRadius * outerRadius) -
            (innerRadius * innerRadius));
    return abs((areaPhase * 2.0) - 1.0);
}

float4 CatalogHalftonePattern(
    float2 uv,
    float4 source)
{
    float cellSize = max(FilterOptions4.x, 2.0);
    float contrastScale = clamp(
        1.0 + (FilterOptions1.x * 0.1),
        0.0,
        16.0);
    float coverage = saturate(
        ((1.0 - CatalogLuminance(source)) - 0.5) *
        contrastScale +
        0.5);
    int patternType = (int)(FilterOptions3.x + 0.5);
    float2 pixel = uv / PixelSize;
    float threshold = patternType == 1
        ? CatalogHalftoneLineThreshold(pixel, cellSize)
        : patternType == 2
            ? CatalogHalftoneCircleThreshold(pixel, cellSize)
            : CatalogHalftoneDotThreshold(pixel, cellSize);
    float ink;
    if (coverage <= 0.0)
    {
        ink = 0.0;
    }
    else if (coverage >= 1.0)
    {
        ink = 1.0;
    }
    else
    {
        float antialiasWidth = clamp(
            fwidth(threshold) * 0.5,
            0.0001,
            0.5);
        ink = 1.0 - smoothstep(
            coverage - antialiasWidth,
            coverage + antialiasWidth,
            threshold);
    }

    float3 pattern = lerp(
        FilterOptions0.rgb,
        FilterOptions2.rgb,
        ink);
    return float4(pattern * source.a, source.a);
}
