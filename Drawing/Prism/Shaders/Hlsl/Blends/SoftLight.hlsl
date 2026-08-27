float3 BlendSoftLight(float3 backdrop, float3 source)
{
    float3 low = backdrop -
        ((1.0 - (2.0 * source)) * backdrop * (1.0 - backdrop));
    float3 polynomial =
        (((16.0 * backdrop) - 12.0) * backdrop + 4.0) * backdrop;
    float3 curve = lerp(
        polynomial,
        sqrt(max(backdrop, 0.0)),
        step(0.25, backdrop));
    float3 high = backdrop +
        (((2.0 * source) - 1.0) * (curve - backdrop));
    return lerp(low, high, step(0.5, source));
}

float4 SoftLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 13);
}
