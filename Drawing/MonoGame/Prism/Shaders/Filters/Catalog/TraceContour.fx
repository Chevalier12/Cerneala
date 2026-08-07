uint TraceContourEdge()
{
    uint low = (uint)FilterOptions0.x;
    uint high = (uint)FilterOptions0.y;
    return (low & 0xffffu) | (high << 16);
}

bool TraceContourSelected(
    float4 color,
    float level,
    bool lower)
{
    float luminance = CatalogLuminance(color);
    return lower
        ? luminance < level
        : luminance >= level;
}

float4 CatalogTraceContour(
    float2 uv,
    float4 source,
    int profile)
{
    const float alphaEpsilon = 0.000001;
    const uint lowerEdge = 1099781210u;
    float level = saturate(FilterOptions1.x);
    bool lower = TraceContourEdge() == lowerEdge;
    bool selected = TraceContourSelected(source, level, lower);
    bool touchesOppositeRegion = false;

    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            if (offsetX == 0 && offsetY == 0)
            {
                continue;
            }

            float4 sample = CatalogLinearSample(
                uv + (float2(offsetX, offsetY) * PixelSize),
                profile);
            if (sample.a > alphaEpsilon &&
                TraceContourSelected(sample, level, lower) != selected)
            {
                touchesOppositeRegion = true;
            }
        }
    }

    bool boundary = source.a > alphaEpsilon &&
        selected &&
        touchesOppositeRegion;
    float value = boundary ? 0.0 : source.a;
    return float4(value, value, value, source.a);
}
