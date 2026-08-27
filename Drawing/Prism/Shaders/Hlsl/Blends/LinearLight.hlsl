float3 BlendLinearLight(float3 backdrop, float3 source)
{
    return backdrop + (2.0 * source) - 1.0;
}

float4 LinearLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 16);
}
