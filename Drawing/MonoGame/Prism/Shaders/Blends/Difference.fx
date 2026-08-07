float3 BlendDifference(float3 backdrop, float3 source)
{
    return abs(backdrop - source);
}

float4 DifferenceBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 19);
}
