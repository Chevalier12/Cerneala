float4 CatalogMorphology(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 stepSize =
        FilterOptions9.xy * PixelSize;
    float4 negative = CatalogLinearSample(
        uv - stepSize,
        profile);
    float4 positive = CatalogLinearSample(
        uv + stepSize,
        profile);
    return filterId == 55
        ? max(source, max(negative, positive))
        : min(source, min(negative, positive));
}

