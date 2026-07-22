float4 CatalogConvolution(
    float2 uv,
    float4 source,
    int profile)
{
    float4 total = 0.0;
    float weightTotal = 0.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 kernelUv =
                (float2(x, y) + 1.5) / 3.0;
            float weight = tex2D(
                SecondaryTextureSampler,
                kernelUv).r;
            total += CatalogLinearSample(
                uv + (float2(x, y) * PixelSize),
                profile) * weight;
            weightTotal += weight;
        }
    }
    float divisor = abs(weightTotal) < 0.000001
        ? 1.0
        : weightTotal;
    float4 result =
        (total / divisor) *
        FilterOptions4.x +
        FilterOptions3.x;
    result.a = lerp(
        source.a,
        result.a,
        step(0.5, FilterOptions0.x));
    return result;
}

