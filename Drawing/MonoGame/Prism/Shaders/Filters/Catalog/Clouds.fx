float4 CatalogClouds(float4 source, float noise)
{
    float3 pattern = lerp(FilterOptions1.rgb, FilterOptions0.rgb, noise);
    return float4(pattern * source.a, source.a);
}
