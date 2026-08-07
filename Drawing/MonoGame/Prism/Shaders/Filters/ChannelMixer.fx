float3 ApplyChannelMixer(float3 color, VertexShaderOutput input)
{
    return float3(
        dot(color, FilterOptions0.rgb) + FilterOptions3.x,
        dot(color, FilterOptions1.rgb) + FilterOptions3.y,
        dot(color, FilterOptions2.rgb) + FilterOptions3.z);
}
