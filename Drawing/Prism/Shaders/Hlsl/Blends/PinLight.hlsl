float3 BlendPinLight(float3 backdrop, float3 source)
{
    float3 low = min(backdrop, 2.0 * source);
    float3 high = max(backdrop, (2.0 * source) - 1.0);
    return lerp(low, high, step(0.5, source));
}

float4 PinLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 17);
}
