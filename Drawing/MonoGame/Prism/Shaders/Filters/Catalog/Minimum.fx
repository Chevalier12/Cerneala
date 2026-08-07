float4 CatalogMinimum(float2 uv, float4 source, int profile)
{
    float2 radius = max(FilterOptions9.xy, 0.0);
    float4 result = source;
    int shape = (int)floor(FilterOptions9.z / 4.0);
    int crossExtent = shape == 1
        ? (int)floor(radius.y + 0.000001)
        : (int)ceil(radius.y);
    for (int cross = -crossExtent; cross <= crossExtent; cross++)
    {
        float remaining = 1.0;
        if (shape == 0)
        {
            float normalizedCross = radius.y == 0.0 ? 0.0 : cross / radius.y;
            remaining = 1.0 - (normalizedCross * normalizedCross);
            if (remaining < 0.0)
            {
                continue;
            }
        }

        int alongExtent = radius.x == 0.0
            ? 0
            : shape == 1
                ? (int)floor(radius.x + 0.000001)
                : (int)floor((radius.x * sqrt(remaining)) + 0.000001);
        for (int along = -alongExtent; along <= alongExtent; along++)
        {
            result = min(
                result,
                CatalogLinearSample(
                    uv + (float2(along, cross) * PixelSize),
                    profile));
        }
    }
    return result;
}
