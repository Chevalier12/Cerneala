float4 ApplySharpen(VertexShaderOutput input, float4 center, int profile)
{
    return NeighborhoodContrastAdaptiveSharpen(
        NeighborhoodUnclampedUv(input),
        center,
        FilterOptions0.x,
        profile);
}
