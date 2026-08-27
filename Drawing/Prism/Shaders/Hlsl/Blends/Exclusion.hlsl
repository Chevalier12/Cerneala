float3 BlendExclusion(float3 backdrop, float3 source)
{
    return backdrop + source - (2.0 * backdrop * source);
}

float4 ExclusionBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 20);
}
