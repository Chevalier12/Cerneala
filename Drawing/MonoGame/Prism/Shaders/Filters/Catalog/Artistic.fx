float4 CatalogArtistic(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float edge = CatalogSobel(uv, profile);
    float3 straight =
        saturate(Unpremultiply(source));
    float amount = saturate(
        0.05 +
        (CatalogParameterMagnitude() * 0.01));
    float noise = NeighborhoodHash(
        uv / PixelSize,
        CatalogSeed()) - 0.5;
    uint variant = ((uint)filterId - 77u) % 6u;
    float3 result = straight;
    if (variant == 0)
    {
        result =
            floor(straight * 6.0 + 0.5) / 6.0 -
            edge * amount;
    }
    else if (variant == 1)
    {
        result = lerp(
            straight,
            CatalogLuminance(source),
            amount);
    }
    else if (variant == 2)
    {
        result =
            floor(
                saturate(straight + noise * amount) *
                8.0 +
                0.5) / 8.0;
    }
    else if (variant == 3)
    {
        result = lerp(
            straight,
            1.0 - edge,
            amount);
    }
    else if (variant == 4)
    {
        result = lerp(
            straight,
            straight * float3(1.1, 0.95, 0.8),
            amount);
    }
    else
    {
        result += float3(
            edge * amount,
            -edge * amount * 0.5,
            noise * amount);
    }
    return float4(saturate(result) * source.a, source.a);
}

