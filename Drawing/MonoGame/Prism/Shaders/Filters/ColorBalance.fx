float3 ApplyColorBalance(float3 color, VertexShaderOutput input)
{
    float luminance = AdjustmentLuminance(color);
    float shadows = 1.0 - smoothstep(0.0, 0.333, luminance);
    float highlights = smoothstep(0.550, 1.0, luminance);
    float midtones = 1.0 - shadows - highlights;
    float3 adjusted = color +
        (FilterOptions0.rgb * shadows) +
        (FilterOptions1.rgb * midtones) +
        (FilterOptions2.rgb * highlights);
    return FilterOptions3.x > 0.5
        ? PreserveAdjustmentLightness(color, adjusted)
        : adjusted;
}
