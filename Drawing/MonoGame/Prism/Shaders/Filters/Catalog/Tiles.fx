float4 CatalogTiles(float2 uv, int profile)
{
    float2 textureSize = 1.0 / PixelSize;
    float2 pixelCenter = uv / PixelSize;
    float cellSize = max(
        1.0,
        max(textureSize.x, textureSize.y) /
        max(FilterOptions4.x, 1.0));
    float2 cell = floor(pixelCenter / cellSize);
    float2 cellOrigin = cell * cellSize;
    float2 cellEnd = min(cellOrigin + cellSize, textureSize);
    float2 cellExtent = cellEnd - cellOrigin;
    uint2 cellIndex = (uint2)cell;
    uint seedLow = (uint)round(FilterOptions3.x);
    uint seedHigh = (uint)round(FilterOptions3.y);
    float2 jitter = float2(
        AddNoiseUniform(cellIndex, seedLow, seedHigh, 0u),
        AddNoiseUniform(cellIndex, seedLow, seedHigh, 1u));
    jitter = (jitter * 2.0) - 1.0;
    float2 samplePixel = pixelCenter -
        (jitter * saturate(FilterOptions2.x) * cellExtent);

    if (any(samplePixel < cellOrigin) || any(samplePixel >= cellEnd))
    {
        return float4(
            FilterOptions0.rgb * FilterOptions0.a,
            FilterOptions0.a);
    }

    float2 firstPixelCenter = ceil(cellOrigin - 0.5) + 0.5;
    float2 lastPixelCenter = ceil(cellEnd - 0.5) - 0.5;
    samplePixel = clamp(
        samplePixel,
        firstPixelCenter,
        lastPixelCenter);
    return CatalogLinearSample(samplePixel * PixelSize, profile);
}
