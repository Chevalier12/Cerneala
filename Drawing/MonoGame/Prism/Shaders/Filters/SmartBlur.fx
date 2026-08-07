float4 ApplySmartBlur(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    float4 blurred = SampleSmartBilateral(
        uv,
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        (int)(FilterOptions1.x + 0.5),
        profile,
        FilterOptions0.y);
    int mode = (int)(FilterOptions0.w + 0.5);
    if (mode == 0)
    {
        return blurred;
    }
    float edge = saturate(length(
        Unpremultiply(center) - Unpremultiply(blurred)));
    float4 edgeColor = float4(edge.xxx * center.a, center.a);
    return mode == 1
        ? edgeColor
        : lerp(center, edgeColor, edge);
}
