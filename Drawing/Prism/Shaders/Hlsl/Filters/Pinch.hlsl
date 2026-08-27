float4 ApplyPinch(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 center = FilterOptions0.yz;
    float2 delta = uv - center;
    float radius = length(delta) * 2.0;
    if (radius == 0.0 || radius >= 1.0)
    {
        return SampleResamplingSource(uv, profile, 0, 0.0);
    }
    float amount =
        0.95 * (FilterOptions0.x / (1.0 + abs(FilterOptions0.x)));
    float sineRadius = sin(1.5707963267948966 * radius);
    float2 mapped = center +
        (delta * pow(max(sineRadius, 1e-20), -amount));
    return SampleResamplingSource(mapped, profile, 0, 0.0);
}
