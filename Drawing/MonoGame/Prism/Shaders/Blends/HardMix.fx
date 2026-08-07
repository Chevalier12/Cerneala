float3 BlendHardMix(float3 backdrop, float3 source)
{
    return step(0.5, BlendVividLight(backdrop, source));
}

float4 HardMixBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 18);
}
