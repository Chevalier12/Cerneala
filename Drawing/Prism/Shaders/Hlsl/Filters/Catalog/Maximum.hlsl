float4 CatalogMaximum(float2 uv, float4 source, int profile)
{
    float2 radius = max(FilterOptions9.xy, 0.0);
    float4 result = source;
    int crossExtent = (int)ceil(radius.y);
    for (int cross = -crossExtent; cross <= crossExtent; cross++)
    {
        float normalizedCross = radius.y == 0.0 ? 0.0 : cross / radius.y;
        float remaining = 1.0 - (normalizedCross * normalizedCross);
        if (remaining < 0.0)
        {
            continue;
        }

        int alongExtent = radius.x == 0.0
            ? 0
            : (int)floor((radius.x * sqrt(remaining)) + 0.000001);
        for (int along = -alongExtent; along <= alongExtent; along++)
        {
            result = max(
                result,
                CatalogLinearSample(
                    uv + (float2(along, cross) * PixelSize),
                    profile));
        }
    }
    return result;
}
