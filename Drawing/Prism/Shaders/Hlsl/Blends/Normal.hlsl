float3 BlendNormal(float3 backdrop, float3 source)
{
    return source;
}

float4 NormalBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 0);
}
