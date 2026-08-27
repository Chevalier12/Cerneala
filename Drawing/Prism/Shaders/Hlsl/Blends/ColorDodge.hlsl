float3 BlendColorDodge(float3 backdrop, float3 source)
{
    float3 denominator = max(1.0 - source, 0.000001);
    float3 result = min(1.0, backdrop / denominator);
    return lerp(result, 1.0, step(1.0, source));
}

float4 ColorDodgeBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 9);
}
