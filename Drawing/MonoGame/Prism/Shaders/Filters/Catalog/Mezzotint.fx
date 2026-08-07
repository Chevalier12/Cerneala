float4 CatalogMezzotint(float2 pixel, float4 source)
{
    float threshold = CatalogMezzotintThreshold(pixel);
    float value = step(threshold, CatalogLuminance(source));
    return float4(
        value * source.a,
        value * source.a,
        value * source.a,
        source.a);
}
