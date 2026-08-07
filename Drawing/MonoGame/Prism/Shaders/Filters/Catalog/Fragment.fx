float4 CatalogFragment(float2 uv, int profile)
{
    float offset = max(FilterOptions9.x, 0.0);
    float2 diagonal = PixelSize * offset;
    return (
        CatalogLinearSample(uv - diagonal, profile) +
        CatalogLinearSample(uv + float2(diagonal.x, -diagonal.y), profile) +
        CatalogLinearSample(uv + float2(-diagonal.x, diagonal.y), profile) +
        CatalogLinearSample(uv + diagonal, profile)) / 4.0;
}
