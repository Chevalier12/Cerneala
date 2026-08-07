float3 BlendOverlay(float3 backdrop, float3 source)
{
    float3 low = 2.0 * backdrop * source;
    float3 high =
        1.0 - (2.0 * (1.0 - backdrop) * (1.0 - source));
    return lerp(low, high, step(0.5, backdrop));
}

float4 OverlayBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 12);
}
