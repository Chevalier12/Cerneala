float3 BlendHue(float3 backdrop, float3 source)
{
    return SetBlendLuminosity(
        SetBlendSaturation(source, BlendSaturation(backdrop)),
        BlendLuminosity(backdrop));
}

float4 HueBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 23);
}
