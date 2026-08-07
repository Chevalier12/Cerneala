float3 BlendScreen(float3 backdrop, float3 source)
{
    return backdrop + source - (backdrop * source);
}

float4 ScreenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 8);
}
