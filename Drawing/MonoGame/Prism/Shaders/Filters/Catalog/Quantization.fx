float4 CatalogQuantization(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 65)
    {
        return (
            (source * 4.0) +
            CatalogLinearSample(
                uv + float2(PixelSize.x, 0.0),
                profile) +
            CatalogLinearSample(
                uv - float2(PixelSize.x, 0.0),
                profile) +
            CatalogLinearSample(
                uv + float2(0.0, PixelSize.y),
                profile) +
            CatalogLinearSample(
                uv - float2(0.0, PixelSize.y),
                profile)) / 8.0;
    }
    else if (filterId == 66)
    {
        float offset = max(FilterOptions0.x, 0.0);
        float2 diagonal = PixelSize * offset;
        return (
            CatalogLinearSample(uv - diagonal, profile) +
            CatalogLinearSample(
                uv + float2(diagonal.x, -diagonal.y),
                profile) +
            CatalogLinearSample(
                uv + float2(-diagonal.x, diagonal.y),
                profile) +
            CatalogLinearSample(uv + diagonal, profile)) /
            4.0;
    }
    else
    {
        float cell = max(
            filterId == 63
                ? FilterOptions1.x
                : FilterOptions0.x,
            1.0);
        float2 cellIndex = floor(pixel / cell);
        float2 center = (cellIndex + 0.5) * cell;
        float seed = CatalogSeed();
        if (filterId == 64 || filterId == 69)
        {
            center =
                (cellIndex +
                    float2(
                        NeighborhoodHash(cellIndex, seed),
                        NeighborhoodHash(cellIndex, seed + 1.0))) *
                cell;
        }
        float4 sampled = CatalogLinearSample(
            center * PixelSize,
            profile);
        if (filterId == 67)
        {
            float noise = NeighborhoodHash(pixel, seed);
            float value =
                step(noise, CatalogLuminance(source));
            return float4(
                value * source.a,
                value * source.a,
                value * source.a,
                source.a);
        }
        if (filterId == 63)
        {
            float dotValue = NeighborhoodHash(cellIndex, 63.0);
            float value =
                step(dotValue, CatalogLuminance(source));
            return float4(
                value * source.a,
                value * source.a,
                value * source.a,
                source.a);
        }
        if (filterId == 69 &&
            distance(pixel, center) > cell * 0.45)
        {
            float4 background = FilterOptions0;
            return float4(
                background.rgb *
                    source.a *
                    background.a,
                source.a * background.a);
        }
        return sampled;
    }
}

