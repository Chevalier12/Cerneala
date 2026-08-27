float4 ApplyDustScratches(VertexShaderOutput input, float4 center, int profile)
{
    return NeighborhoodAdaptiveThresholdedMedian(
        NeighborhoodUnclampedUv(input),
        profile,
        (int)(FilterOptions0.x + 0.5),
        FilterOptions0.y);
}
