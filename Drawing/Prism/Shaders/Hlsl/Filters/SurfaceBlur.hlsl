float4 ApplySurfaceBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleSurfaceBilateral(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(10),
        profile,
        FilterOptions0.y);
}
