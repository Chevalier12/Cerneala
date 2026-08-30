float4 ApplySpinBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleSpinBlur(
        input.Position.xy * PixelSize,
        input.Position.xy,
        (int)(FilterOptions9.z + 0.5),
        profile);
}
