float4 ApplySharpenMore(VertexShaderOutput input, float4 center, int profile)
{
    return NeighborhoodBinomialHighBoost(
        NeighborhoodUnclampedUv(input),
        center,
        FilterOptions0.x,
        profile);
}
