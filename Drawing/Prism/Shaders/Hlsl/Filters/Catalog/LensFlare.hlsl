float4 CatalogLensFlare(float2 uv, float4 source)
{
    float3 flare = tex2D(SecondaryTextureSampler, uv).rgb;
    float3 straight = saturate(Unpremultiply(source) + flare);
    return float4(straight * source.a, source.a);
}
