float4 CatalogRoughPastels(
    float2 uv,
    float4 source,
    int profile)
{
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float detail = clamp(FilterOptions4.x, 0.0, 16.0);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    bool coarsePass = passIndex == 0;
    float4 filteredSample = CatalogPolynomialAnisotropicKuwahara(
        uv,
        source,
        profile,
        radius,
        coarsePass
            ? 2.0 + (detail * 0.45)
            : 4.0 + (detail * 0.5),
        coarsePass ? 1.35 : 1.1,
        coarsePass ? 0.55 : 0.72,
        coarsePass ? 0.22 : 0.08,
        0.0,
        0.0,
        0.0,
        0.0,
        coarsePass ? 0xc4da5ddfu : 0x8321ca5du);
    if (coarsePass)
    {
        return filteredSample;
    }
    float3 filtered = Unpremultiply(filteredSample);

    float2 pixel = uv / PixelSize;
    float scaling = max(FilterOptions3.x, 0.125);
    int textureCode = clamp((int)round(FilterOptions6.x), 0, 3);
    bool invert = FilterOptions0.x >= 0.5;
    float paper = CatalogProceduralTextureHeight(
        pixel,
        textureCode,
        scaling,
        0x2ab93403u,
        0x895bec4bu);
    float heightValue = invert ? 1.0 - paper : paper;

    float3 tensor = CatalogFacetStructureTensor(uv, profile);
    float angle =
        (0.5 * atan2(
            2.0 * tensor.y,
            tensor.x - tensor.z)) +
        (3.14159265 * 0.5);
    float2 tangent = float2(cos(angle), sin(angle));
    float fiber = 0.5 +
        (0.5 * cos(
            (dot(pixel, tangent) /
                max(scaling * 0.75, 0.125)) *
            3.14159265));

    float relief = clamp(FilterOptions2.x, 0.0, 2.0);
    int lightDirection = clamp(
        (int)round(FilterOptions1.x),
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
        0x2ab93403u,
        0x895bec4bu);
    float behind = CatalogProceduralTextureHeight(
        pixel - light,
        textureCode,
        scaling,
        0x2ab93403u,
        0x895bec4bu);
    if (invert)
    {
        ahead = 1.0 - ahead;
        behind = 1.0 - behind;
    }

    float coverageGap =
        (0.55 * heightValue) +
        (0.45 * (1.0 - fiber));
    float coverage = clamp(
        1.0 - (coverageGap * (0.12 + (0.18 * relief))),
        0.55,
        1.0);
    float shade = clamp(
        1.0 + ((ahead - behind) * relief * 1.25),
        0.55,
        1.45);
    float3 result =
        ((filtered * coverage) +
            ((1.0 - coverage) * 0.65)) *
        shade;
    return float4(saturate(result) * source.a, source.a);
}
