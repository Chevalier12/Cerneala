float3 BlendLinearDodge(float3 backdrop, float3 source)
{
    return backdrop + source;
}

float4 LinearDodgeBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 10);
}
