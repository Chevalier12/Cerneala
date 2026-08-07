float3 BlendPassThrough(float3 backdrop, float3 source)
{
    return source;
}

float4 PassThroughBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 27);
}
