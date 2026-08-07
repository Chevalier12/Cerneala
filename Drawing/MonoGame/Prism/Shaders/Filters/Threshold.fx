float3 ApplyThreshold(float3 color, VertexShaderOutput input)
{
    float threshold = tex2D(
        SecondaryTextureSampler,
        float2(0.5, 0.5)).r;
    float value = AdjustmentLuminance(color) > threshold
        ? 1.0
        : 0.0;
    return value.xxx;
}
