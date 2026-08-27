float4 ApplyBoxBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleNeighborhoodLine(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(3),
        profile,
        false);
}
