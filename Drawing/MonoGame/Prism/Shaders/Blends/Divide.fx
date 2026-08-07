float3 BlendDivide(float3 backdrop, float3 source)
{
    return lerp(
        backdrop / max(source, 0.000001),
        1.0,
        step(source, 0.0));
}

float4 DivideBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 22);
}
