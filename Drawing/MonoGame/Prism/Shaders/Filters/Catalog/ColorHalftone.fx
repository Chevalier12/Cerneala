float4 CatalogColorHalftone(float2 pixel, float4 source)
{
    float maxRadius = max(FilterOptions9.x, 0.0);
    if (maxRadius <= 0.0)
    {
        return source;
    }

    float3 straight = saturate(Unpremultiply(source));
    float black = 1.0 - max(
        straight.r,
        max(straight.g, straight.b));
    float colorRange = 1.0 - black;
    float3 cmy = saturate(
        (1.0 - straight - black) /
        max(colorRange, 0.000001));
    float4 coverage = float4(cmy, black);
    float4 rotatedX =
        (FilterOptions2 * pixel.x) -
        (FilterOptions3 * pixel.y);
    float4 rotatedY =
        (FilterOptions3 * pixel.x) +
        (FilterOptions2 * pixel.y);
    float cellSize = sqrt(2.0) * maxRadius;
    float4 localX =
        (frac((rotatedX / cellSize) + 0.5) - 0.5) *
        cellSize;
    float4 localY =
        (frac((rotatedY / cellSize) + 0.5) - 0.5) *
        cellSize;
    float halfCell = cellSize * 0.5;
    float4 squaredDistance = min(
        ((localX * localX) + (localY * localY)) /
            (halfCell * halfCell),
        2.0);
    float circleCoverage = 0.25 * 3.14159265358979323846;
    float4 threshold =
        circleCoverage * min(squaredDistance, 1.0) +
        (1.0 - circleCoverage) *
            max(squaredDistance - 1.0, 0.0);
    float antialiasWidth = clamp(
        0.5 / maxRadius,
        0.0001,
        0.25);
    float4 ink = smoothstep(
        threshold - antialiasWidth,
        threshold + antialiasWidth,
        coverage);
    ink *= step(0.000001, coverage);
    ink = lerp(
        ink,
        1.0,
        step(0.999999, coverage));
    float blackPaper = 1.0 - ink.w;
    float3 result = float3(
        (1.0 - ink.x) * blackPaper,
        (1.0 - ink.y) * blackPaper,
        (1.0 - ink.z) * blackPaper);
    return float4(result * source.a, source.a);
}
