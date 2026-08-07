float4 CatalogSolarize(float4 source)
{
    float3 straight = saturate(Unpremultiply(source));
    float3 inverted = 1.0 - straight;
    straight = lerp(straight, inverted, step(FilterOptions0.x, straight));
    return float4(saturate(straight) * source.a, source.a);
}
