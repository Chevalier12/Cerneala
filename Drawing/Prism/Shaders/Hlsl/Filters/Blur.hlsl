float4 ApplyBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleOptimizedBilinearGaussian(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(1),
        profile);
}
