float3 BlendMultiply(float3 backdrop, float3 source)
{
    return backdrop * source;
}

float4 MultiplyBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 3);
}
