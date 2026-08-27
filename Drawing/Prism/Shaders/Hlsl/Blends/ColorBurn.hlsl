float3 BlendColorBurn(float3 backdrop, float3 source)
{
    float3 denominator = max(source, 0.000001);
    float3 result = 1.0 - min(1.0, (1.0 - backdrop) / denominator);
    return lerp(result, 0.0, step(source, 0.0));
}

float4 ColorBurnBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 4);
}
