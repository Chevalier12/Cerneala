float3 BlendDarkerColor(float3 backdrop, float3 source)
{
    return BlendLuminosity(backdrop) <= BlendLuminosity(source)
        ? backdrop
        : source;
}

float4 DarkerColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 6);
}
