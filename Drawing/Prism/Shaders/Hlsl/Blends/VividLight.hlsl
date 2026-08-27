float3 BlendVividLight(float3 backdrop, float3 source)
{
    float3 low = BlendColorBurn(backdrop, 2.0 * source);
    float3 high = BlendColorDodge(
        backdrop,
        2.0 * (source - 0.5));
    return lerp(low, high, step(0.5, source));
}

float4 VividLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 15);
}
