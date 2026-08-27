float4 ApplyGaussianBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleIncrementalGaussian(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(4),
        profile,
        FilterOptions0.w);
}
