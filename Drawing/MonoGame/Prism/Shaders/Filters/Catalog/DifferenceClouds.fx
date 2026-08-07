float4 CatalogDifferenceClouds(float4 source, float noise)
{
    float3 pattern = lerp(FilterOptions1.rgb, FilterOptions0.rgb, noise);
    pattern = abs(saturate(Unpremultiply(source)) - pattern);
    return float4(pattern * source.a, source.a);
}
