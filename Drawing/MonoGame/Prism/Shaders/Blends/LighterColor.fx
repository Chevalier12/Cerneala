float3 BlendLighterColor(float3 backdrop, float3 source)
{
    return BlendLuminosity(backdrop) >= BlendLuminosity(source)
        ? backdrop
        : source;
}

float4 LighterColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 11);
}
