float3 ApplyColorLookup(float3 color, VertexShaderOutput input)
{
    return SampleAdjustmentLut(color);
}
