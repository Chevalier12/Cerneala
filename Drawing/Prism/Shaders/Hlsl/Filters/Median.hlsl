float4 ApplyMedian(VertexShaderOutput input, float4 center, int profile)
{
    return NeighborhoodMedian3x3(
        NeighborhoodUnclampedUv(input),
        profile);
}
