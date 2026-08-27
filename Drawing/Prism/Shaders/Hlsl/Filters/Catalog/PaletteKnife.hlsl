float4 CatalogPaletteKnife(
    float2 uv,
    float4 source,
    int profile)
{
    float detail = max(FilterOptions1.x, 0.0);
    float softness = max(FilterOptions0.x, 0.0);
    float sharpness = clamp(
        (2.0 + (2.0 * detail)) /
            (1.0 + (0.5 * softness)),
        0.5,
        12.0);
    return CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        max(FilterOptions9.x, FilterOptions9.y),
        sharpness,
        1.0,
        1.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0xd1936bd4u);
}
