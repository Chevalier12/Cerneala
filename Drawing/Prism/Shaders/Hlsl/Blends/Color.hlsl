float3 BlendColorMode(float3 backdrop, float3 source)
{
    return SetBlendLuminosity(source, BlendLuminosity(backdrop));
}

float4 ColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 25);
}
