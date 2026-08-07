float CatalogCrosshatchTransition(
    float coordinate,
    float period,
    float phase,
    float halfWidth,
    float spatialSoftness,
    float darkness,
    float toneThreshold,
    float toneSoftness)
{
    float cycle = frac((coordinate + phase) / period);
    float distance = abs(cycle - 0.5) * period;
    float lineCoverage = 1.0 - smoothstep(
        max(halfWidth - spatialSoftness, 0.0),
        halfWidth + spatialSoftness,
        distance);
    float tone = smoothstep(
        toneThreshold - toneSoftness,
        toneThreshold + toneSoftness,
        darkness);
    return lineCoverage * tone;
}

float4 CatalogCrosshatch(
    float2 uv,
    float4 source)
{
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    float strength = saturate(FilterOptions2.x);
    if (strength <= 0.0)
    {
        return source;
    }

    float3 straight = saturate(Unpremultiply(source));
    float darkness = 1.0 - dot(
        straight,
        float3(0.2126, 0.7152, 0.0722));
    float period = max(abs(FilterOptions0.x), 4.0);
    float sharpness = saturate(FilterOptions1.x / 10.0);
    float halfWidth = clamp(period * 0.075, 0.45, 1.4);
    float spatialSoftness = lerp(1.5, 0.15, sharpness);
    float toneSoftness = lerp(0.18, 0.02, sharpness);
    float2 pixel = uv / PixelSize;
    float rising = (pixel.x + pixel.y) * 0.70710678118;
    float falling = (pixel.x - pixel.y) * 0.70710678118;
    float phaseStep = period / 3.0;

    float clear = 1.0;
    clear *= 1.0 - CatalogCrosshatchTransition(
        rising, period, 0.0, halfWidth, spatialSoftness,
        darkness, 0.06, toneSoftness);
    clear *= 1.0 - CatalogCrosshatchTransition(
        falling, period, 0.0, halfWidth, spatialSoftness,
        darkness, 0.22, toneSoftness);
    clear *= 1.0 - CatalogCrosshatchTransition(
        rising, period, phaseStep, halfWidth, spatialSoftness,
        darkness, 0.38, toneSoftness);
    clear *= 1.0 - CatalogCrosshatchTransition(
        falling, period, phaseStep, halfWidth, spatialSoftness,
        darkness, 0.54, toneSoftness);
    clear *= 1.0 - CatalogCrosshatchTransition(
        rising, period, 2.0 * phaseStep, halfWidth, spatialSoftness,
        darkness, 0.70, toneSoftness);
    clear *= 1.0 - CatalogCrosshatchTransition(
        falling, period, 2.0 * phaseStep, halfWidth, spatialSoftness,
        darkness, 0.86, toneSoftness);

    float3 result = lerp(
        straight,
        saturate(clear),
        strength);
    return float4(result * source.a, source.a);
}
