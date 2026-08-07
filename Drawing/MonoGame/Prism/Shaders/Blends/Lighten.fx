float3 BlendLighten(float3 backdrop, float3 source)
{
    return max(backdrop, source);
}

float4 LightenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 7);
}
