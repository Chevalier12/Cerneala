float4 ApplyMotionBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleNeighborhoodLine(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(6),
        profile,
        true);
}
