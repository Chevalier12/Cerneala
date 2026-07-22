float4 CatalogEdge(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float edge = CatalogSobel(uv, profile);
    if (filterId == 115)
    {
        float angle =
            FilterOptions1.x * 0.01745329252;
        float2 direction =
            float2(cos(angle), sin(angle)) *
            PixelSize *
            max(FilterOptions2.x, 1.0);
        float delta =
            CatalogLuminance(
                CatalogLinearSample(
                    uv + direction,
                    profile)) -
            CatalogLuminance(
                CatalogLinearSample(
                    uv - direction,
                    profile));
        float value = saturate(
            0.5 + (delta * FilterOptions0.x));
        return float4(
            value * source.a,
            value * source.a,
            value * source.a,
            source.a);
    }
    else if (filterId == 117)
    {
        float value = 1.0 - saturate(
            (edge - FilterOptions0.x) /
            max(1.0 - FilterOptions0.x, 0.0001));
        return float4(
            value * source.a,
            value * source.a,
            value * source.a,
            source.a);
    }
    else if (filterId == 118)
    {
        float3 glow = saturate(
            float3(edge * 0.25, edge * 0.6, edge) *
            max(FilterOptions0.x, 1.0));
        return float4(glow * source.a, source.a);
    }
    else if (filterId == 121)
    {
        float value = step(
            max(edge, 0.02),
            abs(
                CatalogLuminance(source) -
                FilterOptions1.x));
        return float4(
            value * source.a,
            value * source.a,
            value * source.a,
            source.a);
    }

    float4 foreground = FilterOptions0;
    float4 background = FilterOptions1;
    float mixValue = saturate(
        CatalogLuminance(source) +
        (edge * 0.5));
    float3 sketch = lerp(
        foreground.rgb,
        background.rgb,
        mixValue);
    float amount = saturate(
        0.35 +
        (CatalogParameterMagnitude() * 0.01));
    return float4(
        lerp(
            saturate(Unpremultiply(source)),
            sketch,
            amount) * source.a,
        source.a);
}

