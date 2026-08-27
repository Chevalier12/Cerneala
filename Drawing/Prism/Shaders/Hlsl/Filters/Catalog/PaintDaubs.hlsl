float4 CatalogPaintDaubs(
    float2 uv,
    float4 source,
    int profile)
{
    int brushType = clamp(
        (int)round(FilterOptions1.x),
        0,
        5);
    float sharpness = clamp(
        FilterOptions2.x,
        0.5,
        10.0);
    float widthScale = 1.0;
    float minorScale = 1.0;
    float roughness = 0.0;
    float luminancePreference = 0.0;
    if (brushType == 1)
    {
        roughness = 0.55;
        luminancePreference = 0.65;
    }
    else if (brushType == 2)
    {
        roughness = 0.55;
        luminancePreference = -0.65;
    }
    else if (brushType == 3)
    {
        widthScale = 1.45;
        minorScale = 0.7;
        sharpness *= 1.35;
    }
    else if (brushType == 4)
    {
        widthScale = 1.55;
        minorScale = 1.05;
        sharpness *= 0.55;
    }
    else if (brushType == 5)
    {
        widthScale = 1.1;
        minorScale = 0.75;
        roughness = 0.85;
        luminancePreference = 1.1;
        sharpness *= 1.6;
    }
    return CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        max(FilterOptions9.x, FilterOptions9.y),
        sharpness,
        widthScale,
        minorScale,
        roughness,
        luminancePreference,
        0.0,
        0.0,
        0.0,
        0x4bfc76fbu);
}
