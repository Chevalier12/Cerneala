
float ScanlinesGeneralizedGaussian(
    float position,
    float footprint,
    float thickness,
    float softness)
{
    float cyclePosition = frac(position);
    float distance = abs(cyclePosition - 0.5);
    float halfWidth = thickness * 0.5;
    float limitedFootprint = min(footprint, 1.0);
    float effectiveHalfWidth = sqrt(
        (halfWidth * halfWidth) +
        (limitedFootprint * limitedFootprint / 12.0));
    float normalizedDistance = distance / effectiveHalfWidth;
    float shape = lerp(12.0, 2.0, softness);
    float hardScan = lerp(-12.0, -0.5, softness);
    return exp2(
        hardScan * pow(normalizedDistance, shape));
}

float ScanlinesPixelCoverage(
    float position,
    float footprint,
    float thickness,
    float softness)
{
    if (thickness <= 0.0)
    {
        return 0.0;
    }
    if (thickness >= 1.0)
    {
        return 1.0;
    }

    return ScanlinesGeneralizedGaussian(
        position,
        footprint,
        thickness,
        softness);
}

float4 CatalogScanlines(float2 uv, float4 source)
{
    float frequency = max(FilterOptions1.x, 1.0);
    float thickness = saturate(FilterOptions5.x);
    float softness = saturate(FilterOptions4.x);
    float position = (uv.y * frequency) + FilterOptions3.x;
    float footprint = frequency * PixelSize.y;
    float coverage = ScanlinesPixelCoverage(
        position,
        footprint,
        thickness,
        softness) * saturate(FilterOptions2.x);
    float4 lineColor = FilterOptions0;
    float3 result = lerp(
        saturate(Unpremultiply(source)),
        lineColor.rgb,
        coverage * lineColor.a);
    return float4(result * source.a, source.a);
}
