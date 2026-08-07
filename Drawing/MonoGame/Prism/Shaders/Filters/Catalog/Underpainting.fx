float4 CatalogUnderpainting(
    float2 uv,
    float4 source,
    int profile)
{
    if (source.a <= 0.000001)
    {
        return 0.0;
    }

    float radius = max(FilterOptions9.x, FilterOptions9.y);
    float4 filteredSample = radius <= 0.000001
        ? source
        : CatalogPolynomialAnisotropicKuwahara(
            uv,
            source,
            profile,
            radius,
            3.0 + (0.35 * min(radius, 12.0)),
            1.35,
            0.55,
            0.08,
            0.0,
            0.0,
            0.0,
            0.0,
            90u * 0x9e3779b9u);
    float3 filtered = saturate(Unpremultiply(filteredSample));
    float2 pixel = uv / PixelSize;
    float scaling = max(FilterOptions4.x, 0.125);
    int textureCode = clamp(
        (int)round(FilterOptions5.x),
        0,
        3);
    bool invert = FilterOptions1.x >= 0.5;
    float heightValue = CatalogProceduralTextureHeight(
        pixel,
        textureCode,
        scaling,
        0x7584a42du,
        0x1f123bb5u);
    if (invert)
    {
        heightValue = 1.0 - heightValue;
    }

    float relief = clamp(FilterOptions3.x, 0.0, 2.0);
    int lightDirection = clamp(
        (int)round(FilterOptions2.x),
        0,
        7);
    float lightAngle =
        (-3.14159265 * 0.5) +
        (lightDirection * 3.14159265 * 0.25);
    float2 light = float2(cos(lightAngle), sin(lightAngle));
    float ahead = CatalogProceduralTextureHeight(
        pixel + light,
        textureCode,
        scaling,
        0x7584a42du,
        0x1f123bb5u);
    float behind = CatalogProceduralTextureHeight(
        pixel - light,
        textureCode,
        scaling,
        0x7584a42du,
        0x1f123bb5u);
    if (invert)
    {
        ahead = 1.0 - ahead;
        behind = 1.0 - behind;
    }

    float coverage = saturate(FilterOptions6.x);
    float textureTone = lerp(
        1.0,
        0.82 + (0.3 * heightValue),
        coverage);
    float shade = clamp(
        1.0 + ((ahead - behind) * relief * 1.5),
        0.55,
        1.45);
    float3 result = filtered * textureTone * shade;
    return float4(saturate(result) * source.a, source.a);
}
