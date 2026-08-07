float4 ApplySharpenEdges(VertexShaderOutput input, float4 center, int profile)
{
    return NeighborhoodSobelGatedContrastAdaptiveSharpen(
        NeighborhoodUnclampedUv(input),
        center,
        FilterOptions0.x,
        FilterOptions0.y,
        profile);
}
