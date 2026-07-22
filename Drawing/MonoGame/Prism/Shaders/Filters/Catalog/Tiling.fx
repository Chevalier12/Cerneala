float4 CatalogTiling(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 133)
    {
        float amount = FilterOptions0.x;
        float2 direction = FilterOptions2.xy;
        direction = length(direction) > 0.0001
            ? normalize(direction)
            : float2(1.0, 0.0);
        float2 offset =
            direction * amount * PixelSize;
        float4 red = CatalogLinearSample(
            uv + offset,
            profile);
        float4 blue = CatalogLinearSample(
            uv - offset,
            profile);
        return float4(
            red.r,
            source.g,
            blue.b,
            source.a);
    }
    else if (filterId == 122)
    {
        float strength = max(FilterOptions3.x, 0.0);
        return (
            (source * 2.0) +
            CatalogLinearSample(
                uv + float2(
                    strength * PixelSize.x,
                    0.0),
                profile) +
            CatalogLinearSample(
                uv + float2(
                    strength * 2.0 * PixelSize.x,
                    0.0),
                profile)) / 4.0;
    }
    else
    {
        float size = filterId == 116
            ? max(FilterOptions5.x, 1.0)
            : max(
                1.0,
                max(
                    1.0 / PixelSize.x,
                    1.0 / PixelSize.y) /
                max(FilterOptions4.x, 1.0));
        float2 cell = floor(pixel / size);
        float2 samplePixel =
            (cell + 0.5) * size;
        float randomOffset =
            NeighborhoodHash(
                cell,
                CatalogSeed()) - 0.5;
        samplePixel += randomOffset *
            size *
            max(FilterOptions2.x, 0.1);
        return CatalogLinearSample(
            samplePixel * PixelSize,
            profile);
    }
}

