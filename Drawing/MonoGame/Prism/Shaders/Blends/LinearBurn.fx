float3 BlendLinearBurn(float3 backdrop, float3 source)
{
    return backdrop + source - 1.0;
}

float4 LinearBurnBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 5);
}
