float4 CatalogVideo(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    if (filterId == 75)
    {
        float oddLine = fmod(floor(pixel.y), 2.0);
        float4 interpolation = (
            CatalogLinearSample(
                uv - float2(0.0, PixelSize.y),
                profile) +
            CatalogLinearSample(
                uv + float2(0.0, PixelSize.y),
                profile)) * 0.5;
        return lerp(source, interpolation, oddLine);
    }
    else if (filterId == 76)
    {
        float3 straight =
            saturate(Unpremultiply(source));
        float luminance = dot(
            straight,
            float3(0.299, 0.587, 0.114));
        float3 limited = saturate(
            luminance +
            clamp(
                straight - luminance,
                -(1.0 - luminance),
                1.0 - luminance));
        return float4(limited * source.a, source.a);
    }

    float frequency = max(FilterOptions1.x, 1.0);
    float scanlinePosition = frac(
        (uv.y * frequency) +
        FilterOptions4.x);
    float coverage =
        step(scanlinePosition, saturate(FilterOptions5.x)) *
        saturate(FilterOptions2.x);
    float4 lineColor = FilterOptions0;
    float3 result = lerp(
        saturate(Unpremultiply(source)),
        lineColor.rgb,
        coverage * lineColor.a);
    return float4(result * source.a, source.a);
}

