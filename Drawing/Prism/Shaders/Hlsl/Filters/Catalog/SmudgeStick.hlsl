float4 CatalogSmudgeStick(
    float2 uv,
    float4 source,
    int profile)
{
    float amount = saturate(FilterOptions1.x / 10.0);
    if (source.a <= 0.000001 || amount <= 0.0)
    {
        return source.a <= 0.000001 ? 0.0 : source;
    }

    float4 filteredSample = CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        max(FilterOptions9.x, FilterOptions9.y),
        3.0 + (4.0 * amount),
        1.65,
        0.42,
        0.0,
        -0.65 * amount,
        0.8,
        0.0,
        0.0,
        0x1e054218u);
    float3 straight = saturate(Unpremultiply(source));
    float3 filtered = saturate(Unpremultiply(filteredSample));
    float darkness = 1.0 - dot(
        straight,
        float3(0.2126, 0.7152, 0.0722));
    float smudgeMix = amount * (0.55 + (0.45 * darkness));
    float3 result = lerp(straight, filtered, smudgeMix);

    float highlightArea = saturate(FilterOptions0.x / 20.0);
    if (highlightArea > 0.0)
    {
        float threshold = 1.0 - (0.75 * highlightArea);
        float highlightMask = smoothstep(
            threshold,
            threshold + 0.2,
            dot(result, float3(0.2126, 0.7152, 0.0722)));
        float highlightGain =
            highlightMask *
            amount *
            (0.15 + (0.2 * highlightArea));
        result = lerp(result, 1.0, highlightGain);
    }

    return float4(saturate(result) * source.a, source.a);
}
