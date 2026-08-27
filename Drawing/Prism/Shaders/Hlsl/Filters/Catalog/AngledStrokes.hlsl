float4 CatalogAngledStrokes(
    float2 uv,
    float4 source,
    int profile)
{
    return CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        max(max(FilterOptions9.x, FilterOptions9.y), 1.0),
        clamp(FilterOptions2.x, 0.5, 12.0),
        1.65,
        0.42,
        0.0,
        0.0,
        1.0,
        saturate(FilterOptions0.x),
        1.0,
        93u * 0x9e3779b9u);
}
