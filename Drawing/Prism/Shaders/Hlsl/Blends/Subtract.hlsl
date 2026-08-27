float3 BlendSubtract(float3 backdrop, float3 source)
{
    return backdrop - source;
}

float4 SubtractBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 21);
}
