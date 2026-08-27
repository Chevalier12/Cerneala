float3 BlendHardLight(float3 backdrop, float3 source)
{
    return BlendOverlay(source, backdrop);
}

float4 HardLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 14);
}
