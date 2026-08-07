float4 CutoutMeanShift(float2 uv, int profile)
{
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.0)
    {
        return 0.0;
    }

    float3 centerColor = saturate(Unpremultiply(center));
    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float fidelity = saturate(FilterOptions2.x / 10.0);
    float rangeSigma = 0.42 - (0.36 * fidelity);
    float rangeDivisor =
        2.0 * rangeSigma * rangeSigma;
    float3 accumulated = 0.0;
    float totalWeight = 0.0;
    for (int offsetY = -2; offsetY <= 2; offsetY++)
    {
        for (int offsetX = -2; offsetX <= 2; offsetX++)
        {
            float2 offset =
                float2(offsetX, offsetY) *
                radius *
                PixelSize *
                0.5;
            float4 sample =
                CatalogLinearSample(uv + offset, profile);
            float3 sampleColor =
                saturate(Unpremultiply(sample));
            float spatialDistance =
                (offsetX * offsetX) +
                (offsetY * offsetY);
            float3 difference =
                sampleColor - centerColor;
            float rangeDistance =
                dot(difference, difference);
            float weight =
                exp(-spatialDistance / 3.125) *
                exp(-rangeDistance / rangeDivisor) *
                exp(-abs(sample.a - center.a) * 8.0) *
                step(0.000001, sample.a);
            accumulated += sampleColor * weight;
            totalWeight += weight;
        }
    }

    float3 shifted = accumulated /
        max(totalWeight, 0.000001);
    return float4(saturate(shifted) * center.a, center.a);
}

float4 CutoutQuantize(float4 source)
{
    if (source.a <= 0.0)
    {
        return 0.0;
    }

    float levels = clamp(
        floor(FilterOptions0.x + 0.5),
        2.0,
        32.0);
    float scale = levels - 1.0;
    float3 straight =
        saturate(Unpremultiply(source));
    float3 quantized =
        round(straight * scale) / scale;
    return float4(quantized * source.a, source.a);
}

float4 CutoutOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            FilterAuxiliaryTextureSampler,
            clamp(
                uv,
                PixelSize * 0.5,
                1.0 - (PixelSize * 0.5))),
        profile);
}
