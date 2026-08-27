float4 CatalogMorphology(float2 uv, float4 source, int filterId, int profile)
{
    return filterId == 55
        ? CatalogMaximum(uv, source, profile)
        : CatalogMinimum(uv, source, profile);
}

float4 CatalogQuantization(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 63 || filterId == 65)
    {
        return source;
    }
    if (filterId == 64)
    {
        return CatalogCrystallize(
            pixel - 0.5,
            max(FilterOptions0.x, 1.0),
            profile);
    }
    if (filterId == 66)
    {
        return CatalogFragment(uv, profile);
    }
    if (filterId == 67)
    {
        return CatalogMezzotint(pixel, source);
    }
    if (filterId == 68)
    {
        return CatalogMosaic(pixel, profile);
    }
    if (filterId == 69)
    {
        return CatalogPointillize(
            pixel - 0.5,
            max(FilterOptions0.x, 1.0),
            profile);
    }

    float cell = max(FilterOptions0.x, 1.0);
    float2 center = (floor(pixel / cell) + 0.5) * cell;
    return CatalogLinearSample(center * PixelSize, profile);
}

float4 CatalogProcedural(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    float packedPass = floor(FilterOptions9.z / 4.0);
    float seed = CatalogSeed() + (packedPass * 4099.0);
    float noise = NeighborhoodHash(pixel, seed);
    if (filterId == 70)
    {
        return CatalogClouds(source, noise);
    }
    if (filterId == 71)
    {
        return CatalogDifferenceClouds(source, noise);
    }
    if (filterId == 72)
    {
        return CatalogFibers(pixel, source);
    }
    if (filterId == 73)
    {
        return CatalogLensFlare(uv, source);
    }
    if (filterId == 74)
    {
        return CatalogLightingEffects(uv, source);
    }
    if (filterId == 106)
    {
        return CatalogHalftonePattern(uv, source);
    }
    if (filterId == 114)
    {
        return CatalogDiffuse(uv, source, profile, noise);
    }

    float angle = noise * 6.28318530718;
    float2 offset = float2(cos(angle), sin(angle)) *
        FilterOptions9.xy * PixelSize;
    return CatalogLinearSample(uv + offset, profile);
}

float4 CatalogVideo(float2 uv, float4 source, int filterId, int profile)
{
    return filterId == 76
        ? CatalogNtscColors(source)
        : CatalogScanlines(uv, source);
}

float4 CatalogArtistic(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    if (filterId == 80) return CatalogFilmGrain(uv, source);
    if (filterId == 83) return CatalogPaintDaubs(uv, source, profile);
    if (filterId == 84) return CatalogPaletteKnife(uv, source, profile);
    if (filterId == 85) return CatalogPlasticWrap(uv, source, profile);
    if (filterId == 87) return CatalogRoughPastels(uv, source, profile);
    if (filterId == 88) return CatalogSmudgeStick(uv, source, profile);
    if (filterId == 89) return CatalogSponge(uv, source, profile);
    if (filterId == 90) return CatalogUnderpainting(uv, source, profile);
    if (filterId == 93) return CatalogAngledStrokes(uv, source, profile);
    if (filterId == 94) return CatalogCrosshatch(uv, source);

    float edge = CatalogSobel(uv, profile);
    float3 straight = saturate(Unpremultiply(source));
    float amount = saturate(0.05 + (CatalogParameterMagnitude() * 0.01));
    float noise = NeighborhoodHash(uv / PixelSize, CatalogSeed()) - 0.5;
    uint variant = ((uint)filterId - 77u) % 6u;
    float3 result = straight;
    if (variant == 0)
    {
        result = floor(straight * 6.0 + 0.5) / 6.0 - edge * amount;
    }
    else if (variant == 1)
    {
        result = lerp(straight, CatalogLuminance(source), amount);
    }
    else if (variant == 2)
    {
        result = floor(saturate(straight + noise * amount) * 8.0 + 0.5) / 8.0;
    }
    else if (variant == 3)
    {
        result = lerp(straight, 1.0 - edge, amount);
    }
    else if (variant == 4)
    {
        result = lerp(straight, straight * float3(1.1, 0.95, 0.8), amount);
    }
    else
    {
        result += float3(edge * amount, -edge * amount * 0.5, noise * amount);
    }
    return float4(saturate(result) * source.a, source.a);
}

float4 CatalogEdge(float2 uv, float4 source, int filterId, int profile)
{
    if (filterId == 115)
    {
        return CatalogEmboss(uv, source, profile);
    }

    if (filterId == 117)
    {
        float findEdgesGradient = CatalogScharr(uv, profile);
        float value = 1.0 - saturate(
            (findEdgesGradient - FilterOptions0.x) /
            max(1.0 - FilterOptions0.x, 0.0001));
        return float4(value * source.a, value * source.a, value * source.a, source.a);
    }

    float edge = CatalogSobel(uv, profile);
    float mixValue = saturate(CatalogLuminance(source) + (edge * 0.5));
    float3 sketch = lerp(FilterOptions0.rgb, FilterOptions1.rgb, mixValue);
    float amount = saturate(0.35 + (CatalogParameterMagnitude() * 0.01));
    return float4(
        lerp(saturate(Unpremultiply(source)), sketch, amount) * source.a,
        source.a);
}

float4 CatalogTiling(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    if (filterId == 133)
    {
        return CatalogChromaticAberration(uv, source, profile);
    }
    return CatalogTiles(uv, profile);
}

float4 CatalogColor(float4 source, int filterId)
{
    if (filterId == 119)
    {
        return CatalogSolarize(source);
    }

    if (filterId == 131)
    {
        return CatalogColorMatrix(source);
    }

    if (filterId == 132)
    {
        return CatalogApplyColor(source);
    }

    float3 straight = saturate(Unpremultiply(source));
    return float4(saturate(straight) * source.a, source.a);
}
