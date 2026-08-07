float3 ApplyCurves(float3 color, VertexShaderOutput input)
{
    return AdjustmentCurves(color);
}
