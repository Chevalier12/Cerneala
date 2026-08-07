float4 ApplyBlurMore(VertexShaderOutput input, float4 center, int profile)
{
    return SampleOptimizedBilinearGaussian(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(2),
        profile);
}
