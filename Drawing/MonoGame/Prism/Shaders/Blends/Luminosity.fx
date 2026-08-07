float3 BlendLuminosityMode(float3 backdrop, float3 source)
{
    return SetBlendLuminosity(backdrop, BlendLuminosity(source));
}

float4 LuminosityBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 26);
}
