float4 CatalogTexture(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float textureValue =
        FilterHeader.w >= 1.0
            ? dot(
                tex2D(
                    SecondaryTextureSampler,
                    uv).rgb,
                float3(0.2126, 0.7152, 0.0722))
            : NeighborhoodHash(
                uv / PixelSize,
                CatalogSeed());
    float edge = CatalogSobel(uv, profile);
    float relief = saturate(
        0.05 +
        (CatalogParameterMagnitude() * 0.002));
    float3 result =
        saturate(Unpremultiply(source)) +
        float3(
            (textureValue - 0.5) * relief,
            (edge - 0.5) * relief * 0.5,
            (textureValue - edge) * relief * 0.35);
    return float4(saturate(result) * source.a, source.a);
}

