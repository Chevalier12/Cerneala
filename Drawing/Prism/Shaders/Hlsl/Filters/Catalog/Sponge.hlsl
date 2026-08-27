float4 CatalogSponge(
    float2 uv,
    float4 source,
    int profile)
{
    float definition = clamp(FilterOptions1.x, 0.0, 24.0);
    float smoothness = saturate(FilterOptions2.x / 15.0);
    float sharpness = clamp(
        (1.0 + (0.45 * definition)) *
            (1.25 - (0.5 * smoothness)),
        0.5,
        12.0);
    float minorScale = 0.38 + (0.52 * smoothness);
    return CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        max(max(FilterOptions9.x, FilterOptions9.y), 1.0),
        sharpness,
        1.2,
        minorScale,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        89u * 0x9e3779b9u);
}
