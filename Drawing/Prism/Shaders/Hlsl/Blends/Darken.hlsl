float3 BlendDarken(float3 backdrop, float3 source)
{
    return min(backdrop, source);
}

float4 DarkenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 2);
}
