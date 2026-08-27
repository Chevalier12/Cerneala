float3 ApplyVibrance(float3 color, VertexShaderOutput input)
{
    return AdjustmentVibrance(color);
}
