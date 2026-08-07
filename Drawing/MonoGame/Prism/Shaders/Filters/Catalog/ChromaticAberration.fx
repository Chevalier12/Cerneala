float4 CatalogChromaticAberration(float2 uv, float4 source, int profile)
{
    float amount = FilterOptions0.x;
    float2 direction = FilterOptions2.xy;
    direction = length(direction) > 0.0001
        ? normalize(direction)
        : float2(1.0, 0.0);
    if (FilterOptions3.x > 0.5)
    {
        float2 center = FilterOptions1.xy;
        direction *= distance(uv, center) * 2.0;
    }
    float2 offset = direction * amount * PixelSize;
    float4 red = CatalogLinearSample(uv + offset, profile);
    float4 blue = CatalogLinearSample(uv - offset, profile);
    float alpha = max(source.a, max(red.a, blue.a));
    return float4(red.r, source.g, blue.b, alpha);
}
