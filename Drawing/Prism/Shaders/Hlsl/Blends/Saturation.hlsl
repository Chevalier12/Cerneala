float3 BlendSaturationMode(float3 backdrop, float3 source)
{
    return SetBlendLuminosity(
        SetBlendSaturation(backdrop, BlendSaturation(source)),
        BlendLuminosity(backdrop));
}

float4 SaturationBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 24);
}
